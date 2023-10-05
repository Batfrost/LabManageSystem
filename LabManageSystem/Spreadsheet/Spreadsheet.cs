using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpreadsheetUtilities;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using SS;
using System.Formats.Asn1;
using System.Xml.Linq;

namespace SS
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Spreadsheet : AbstractSpreadsheet
    {
        [JsonProperty]
        private Dictionary<string, Cell> cells = new();
        
        private Dictionary<string, object> cellValues = new();
        
        
        private DependencyGraph dg = new ();
        private Func<string, bool> isValid;
        private Func<string, string> normalize;
        private string version;
        private bool change;
        private Spreadsheet? IDList;
        

        /// <summary>
        /// Creates an empty Spreadsheet with basic isValid and normalize delegates, and version will be "default".
        /// </summary>
        public Spreadsheet() : this(s => true, s => s, "default")
        {
        }

        /// <summary>
        /// Creates a Spreadsheet that uses the given isValid and normalize delegates for variables, and use a given version.
        /// </summary>
        /// <param name="isValid">Delegate for checking whether a variable name is valid</param>
        /// <param name="normalize">Delegate for 'normalizing' all the variables.</param>
        /// <param name="version">Which version of this Spreadsheet software is being used</param>
        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
            this.isValid = isValid;
            this.normalize = normalize;
            this.version = version;
            Changed = false;

        }

        /// <summary>
        /// Searches a given filepath and will 'load' a saved Spreadsheet.
        /// </summary>
        /// <param name="filePath">The path that searches the system for a saved spreasheet file.</param>
        /// <param name="isValid">Delegate for checking whether a variable name is valid</param>
        /// <param name="normalize">Delegate for 'normalizing' all variable names.</param>
        /// <param name="version">The version of the software that the saved file is using.</param>
        public Spreadsheet(string filePath, Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
            this.isValid = isValid;
            this.normalize = normalize;
            this.version = version;

            Spreadsheet? sprdsht;

            //Attempt to load the file and catch any expected exceptions while doing so, and instead throw a SpreadsheetReadWriteException.
            try
            {
                //The software can accept two file types - the custom json made .sprd file, and then .csv file type (comma delimiter file)
                if (filePath.Contains(".sprd"))
                {
                    sprdsht = JsonConvert.DeserializeObject<Spreadsheet>(File.ReadAllText(filePath));
                    if (sprdsht is not null)
                    {
                        //Check if the version is the same
                        if (this.version != sprdsht.Version)
                            throw new SpreadsheetReadWriteException("The Versions of the Spreadsheet software are not the same.");
                        //Check that for each cell, the name is valid and there are no circular exceptions.
                        foreach (KeyValuePair<string, Cell> cell in sprdsht.cells)
                        {
                            try { this.SetContentsOfCell(cell.Key, cell.Value.stringForm); }
                            catch (InvalidNameException) { throw new SpreadsheetReadWriteException("There are invalid variable names in the file."); }
                            catch (CircularException) { throw new SpreadsheetReadWriteException("The loaded spreadsheet file's cells has a circular exception."); }
                            catch (FormulaFormatException) { throw new SpreadsheetReadWriteException("Some of the given formulas are not valid formulas."); }
                        }
                    }
                }
                //The .csv file will have vars separated by a ',' and in normal csv format an actual ',' inside will have that var surrounded by "". 
                //For actual vars surrounded by ", the " will have "" - double quotes.
                else if (filePath.Contains(".csv"))
                {
                    //Start from a new sprdsht and add the read contents.
                    sprdsht = new Spreadsheet();
                    StreamReader csvReader = new StreamReader(filePath);
                    //These vars will cycle through the cell's names
                    char cellNameLetter = 'A';
                    int cellNameNum = 1;
                    string cellName = cellNameLetter + "" + cellNameNum;
                    string line;
                    bool commaExists = false;
                    bool commaSectionDone = true;
                    string temp = "";
                    string cell;
                    while (!csvReader.EndOfStream)
                    {
                        line = csvReader.ReadLine()!;
                        //Go through the row
                        foreach (string rawCell in line.Split(','))
                        {
                            cellName = cellNameLetter + "" + cellNameNum.ToString();
                            //The double "" will actually be " in csv format, for now they will become ")~"
                            cell = rawCell.Replace("\"\"", ")~");

                            //Then if there is still a " inside this section of the cell, that means there is a ',' 
                            //That is separating this cell and the next cell, will be checked with commaExists bool.
                            if (cell.Contains("\"") && !commaExists)
                            {
                                commaExists = true;
                                //Temp var will hold this first part of the cell without that formatted in extra "
                                temp = cell.Replace("\"", "");
                                commaSectionDone = false;
                                continue;
                            } else if (commaExists)
                            {
                                //First the " will be deleted - if there are multiple ", only the first will be removed.
                                if (cell.Contains("\""))
                                {
                                    int t = cell.IndexOf("\"");
                                    cell = cell.Remove(t, 1);
                                    commaSectionDone = true;
                                }

                                //Now temp will gain the ',' the cell was missing, and get the next part.
                                temp +=  "," + cell;

                                //If there is still a " in this cell part, that means there is more parts, continue to next iteration
                                if (commaSectionDone)
                                {
                                    //This is the last part of the comma's separated cells that got split up 
                                    commaExists = false;
                                    cell = temp;
                                } 
                                else
                                    continue;
                                
                            }
                            //Put back the quotes that were originally supposed to be in the cell
                            cell = cell.Replace(")~", "\"");

                            //Then add the cell to the spreadsheet
                            this.SetContentsOfCell(cellName, cell);

                            //For rows, the letter will increment. A -> B ->..., and the number will stay the same.
                            cellNameLetter++;
                        }
                        //Reset the letter back to A, and increment to the next row.
                        cellNameLetter = 'A'; 
                        cellNameNum++;
                        
                    }
                    //Make sure to close the file after we have read all the data from it.
                    csvReader.Close();
                    
                }
            }
            //IOException will catch FileNotFoundExceptions, DirectoryNotFoundExceptions, etc.
            catch (IOException)
            {
                throw new SpreadsheetReadWriteException("The file path was not able to be located.");
            }
            catch (JsonReaderException)
            {
                throw new SpreadsheetReadWriteException("The file is not of a supported type.");
            }
            catch (ArgumentException)
            {
                throw new SpreadsheetReadWriteException("The given filepath is not a correct file path.");
            }
            
            //We have saved, so set Changed to false
            Changed = false;

        }

        public override bool Changed { get => change; protected set => change = value; }

        /// <summary>
        /// If name is invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
        /// value should be either a string, a double, or a Formula.
        /// </summary>
        /// <param name="name">Name of cell whose contents are being retrieved.</param>
        /// <returns>The contents of the named cell</returns>
        public override object GetCellContents(string name)
        {

            //If name is not valid, helper method will throw InvalidNameException.
            name = checkNameThenNormalize(name);

            if (cells.ContainsKey(name))
                return cells[name].contents;
            return "";
        }

        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        /// <returns>An IEnumerable object that will contain all the names of the nonempty cells.</returns>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            //cells will hold all nonempty cells, so if we return the keys, which are the names
            //Then that would return all names of the Nonempty cells.
            return cells.Keys;
        }

        /// <summary>
        /// If name is invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes number.  The method returns a
        /// list consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        protected override IList<string> SetCellContents(string name, double number)
        {
            //The name should already be checked for validity and normalized from the driver method.

            SetCell(name, number);

            return GetCellsToRecalculate(name).ToList();
        }

        /// <summary>
        /// If name is invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes text.  The method returns a
        /// list consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <returns>The List of cells that need to be recalculated</returns>
        protected override IList<string> SetCellContents(string name, string text)
        {
            //The name should already be checked for validity and normalized from the driver method.

            SetCell(name, text);

            return GetCellsToRecalculate(name).ToList();
        }

        /// <summary>
        /// If name is invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if changing the contents of the named cell to be the formula would cause a 
        /// circular dependency, throws a CircularException, and no change is made to the spreadsheet.
        /// 
        /// Otherwise, the contents of the named cell becomes formula.  The method returns a
        /// list consisting of name plus the names of all other cells whose value depends,
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        /// <param name="name">The name of the cell</param>
        /// <param name="formula">The Cell's contents</param>
        /// <returns></returns>
        protected override IList<string> SetCellContents(string name, Formula formula)
        {
            //The name should already be checked for validity and normalized from the driver method.

            //Check if the formula's variables are considered valid here
            //get variables
            IEnumerable<String> variables = formula.GetVariables();
            //for reach variable check if the name is valid
            foreach (string s in variables)
            {
                try
                {
                    checkNameThenNormalize(s);
                }
                catch(InvalidNameException)
                {
                    //if not valid throw exception
                    throw new FormulaFormatException("invalid variables in formula");
                }

            }

            dg.ReplaceDependees(name, formula.GetVariables());

            List<string> listOfDependency = GetCellsToRecalculate(name).ToList();

            SetCell(name, formula);

            return listOfDependency;
        }

        /// <summary>
        /// Private helper method that will add a new cell to cells, and will remove a cell from cells if 
        /// a non empty cell that is in cells is getting changed back to "".
        /// </summary>
        /// <param name="name">The name of the cell getting set</param>
        /// <param name="data">The data that will be inside the cell</param>
        private void SetCell(string name, object data)
        {
            //Check to see if a nonempty cell is getting set back to empty, and if so remove that cell from the list.
            if (cells.ContainsKey(name) && data.ToString() == "")
            {
                cells.Remove(name);
                //Check also if cellValues contains the cell that is getting set to ""
                if (cellValues.ContainsKey(name))
                {
                    dg.ReplaceDependees(name, new List<string>() { "" });
                    cellValues.Remove(name);
                }
                    
                return;
            }
            //Check to see if there is already a cell with this name, and if so, just change it's contents.
            else if (cells.ContainsKey(name))
            {
                cells[name] = new Cell(name, data);
                //Check if data can be or is a double and if cellValues already contains the cell.
                if (data is double || data is Formula)
                {
                    //If it is a formula, then update the dependency graph with the new variables, if there are any
                    if (data is Formula)
                    {
                        Formula formula = (Formula)data;
                        dg.ReplaceDependees(name, formula.GetVariables());
                        cellValues[name] = formula.Evaluate(lookup);
                    } 
                    else
                    {
                        dg.ReplaceDependees(name, new List<string>() { "" });
                        cellValues[name] = data;
                    }
                        

                }
                    
                //Also check if the cell is changing from something that could be a double to a string.
                else if (!(data is double || data is Formula) && cellValues.ContainsKey(name))
                {
                    cellValues[name] = data;
                    dg.ReplaceDependees(name, new List<string>(){""});
                }
                    
                return;
            }
                
            //Then if data is not an empty string, create a new cell and add it to the cells.
            else if (data.ToString() != "")
            {
                cells.Add(name, new Cell(name, data));
                cellValues.Add(name, data);
                    
                
                return;
            }
                
            //If it is "", then don't add it.
        }

        /// <summary>
        /// Returns an enumeration, without duplicates, of the names of all cells whose
        /// values depend directly on the value of the named cell.  In other words, returns
        /// an enumeration, without duplicates, of the names of all cells that contain
        /// formulas containing name.
        /// 
        /// For example, suppose that
        /// A1 contains 3
        /// B1 contains the formula A1 * A1
        /// C1 contains the formula B1 + A1
        /// D1 contains the formula B1 - C1
        /// The direct dependents of A1 are B1 and C1
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override IEnumerable<string> GetDirectDependents(string name)
        {
            return dg.GetDependents(name);
        }

        /// <summary>
        /// Writes the contents of this spreadsheet to the named file using a JSON format.
        /// The JSON object should have the following fields:
        /// "Version" - the version of the spreadsheet software (a string)
        /// "cells" - an object containing 0 or more cell objects
        ///           Each cell object has a field named after the cell itself 
        ///           The value of that field is another object representing the cell's contents
        ///               The contents object has a single field called "stringForm",
        ///               representing the string form of the cell's contents
        ///               - If the contents is a string, the value of stringForm is that string
        ///               - If the contents is a double d, the value of stringForm is d.ToString()
        ///               - If the contents is a Formula f, the value of stringForm is "=" + f.ToString()
        /// 
        /// For example, if this spreadsheet has a version of "default" 
        /// and contains a cell "A1" with contents being the double 5.0 
        /// and a cell "B3" with contents being the Formula("A1+2"), 
        /// a JSON string produced by this method would be:
        /// 
        /// {
        ///   "cells": {
        ///     "A1": {
        ///       "stringForm": "5"
        ///     },
        ///     "B3": {
        ///       "stringForm": "=A1+2"
        ///     }
        ///   },
        ///   "Version": "default"
        /// }
        /// 
        /// If there are any problems opening, writing, or closing the file, the method should throw a
        /// SpreadsheetReadWriteException with an explanatory message.
        /// </summary>
        /// <param name="filename">The name that the file will be saved as.</param>
        public override void Save(string filename)
        {
            try
            {
                if (!System.IO.Directory.Exists(filename))
                    System.IO.Directory.CreateDirectory(filename.Substring(0, filename.LastIndexOf('\\')));
                
                if (filename.Contains(".csv"))
                {
                    string fileAsCSV = "";
                    
                    //This will hold the current cell in the coming loop.
                    string currCell = "";
                    //These will hold the distance between this cell and the next cell
                    int numDist = 0;
                    int lettDist = 0;

                    //The cells dictionary can't be sorted, and the cell names should be sorted by col-row order 
                    List<string> sortedKeys = cells.Keys.Reverse().ToList();
                    //Reverse the keys to col-row order. A1 -> 1A
                    for (int i = 0; i < sortedKeys.Count; i++)
                        sortedKeys[i] = new string(sortedKeys[i].Reverse().ToArray());
                    //Sort and then reverse back, now the keys used below will be grabbed by col-row order.
                    sortedKeys.Sort();
                    for (int i = 0; i < sortedKeys.Count; i++)
                        sortedKeys[i] = new string(sortedKeys[i].Reverse().ToArray());

                    //Go through all cells and add their contents as strings to fileAsCSV, separated by ','s
                    //If any cells already contain a ",", then surround that cell with "", if a cell's contents 
                    //already have a ", in it, put in a bonus " next to it to escape the char " in csv format.
                    for (int i = 0; i < cells.Count; i++)
                    {
                        currCell = cells[sortedKeys[i]].stringForm.Replace("\"", "\"\"");
                        if (currCell.Contains(',')) 
                            currCell = "\"" + currCell + "\"";
                        //Now that this cell's in the correct format, insert it into the file string
                        fileAsCSV += currCell + ",";
                        
                        //Check if there is another cell, and if so get the distance between this cell and the next.
                        if (i != cells.Count - 1)
                        {
                            numDist = int.Parse(sortedKeys[i + 1].Substring(1)) - int.Parse(sortedKeys[i].Substring(1));
                            lettDist = sortedKeys[i + 1].First() - sortedKeys[i].First();
                        }
                        //For this first row/the row the current cell is on, insert "," numDist-1 times
                        for (int v = 0; v < lettDist - 1; v++)
                            fileAsCSV += ",";
                        
                        //For the empty cells inbetween this cell and the next cell in the list, insert just ","
                        for (int j = 0; j < numDist; j++)
                        {
                            for (int v = 0; v < lettDist - 1; v++)
                            {
                                fileAsCSV += ",";
                            }
                            //New row, new line
                            fileAsCSV += "\n";
                        }
                        lettDist = 0;
                        numDist = 0;
                           
                    }

                    File.WriteAllText(filename, fileAsCSV);
                }
                   
                else
                    File.WriteAllText(filename, JsonConvert.SerializeObject(this));
                
            }
            catch (DirectoryNotFoundException)
            {
                throw new SpreadsheetReadWriteException("The path the file was trying to save to does not exist.");
            }
            catch (IOException e)
            {
                throw new SpreadsheetReadWriteException(e.Message);
            }
            catch (ArgumentException)
            {
                throw new SpreadsheetReadWriteException("There was a problem saving the file to the specified path.");
            }
            

            Changed = false;

        }

        /// <summary>
        /// If name is invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the value (as opposed to the contents) of the named cell.  The return
        /// value should be either a string, a double, or a SpreadsheetUtilities.FormulaError.
        /// </summary>
        /// <param name="name">The name of the cell whose value is getting retrieved</param>
        /// <returns>The value of the named cell</returns>
        public override object GetCellValue(string name)
        {
            //Check name, and if valid change it to its normalized form.
            name = checkNameThenNormalize(name);
            //Check if the cell is not empty
            if (cells.ContainsKey(name))
            {
                return cellValues[name];
            }
            //Variable is empty - not in cells, so return "".
            return "";
        }

        private double lookup(string name)
        {
            //First check whether it is an empty cell or not and if not, then see if it has been evaluated as a double.
            if (cellValues.ContainsKey(name) && cellValues[name] is double)
                return (double)cellValues[name];

            //If the cell getting looked up is unknown (empty cells and strings count here)...
            //Or they are FormulaErrors, then throw an exception for these unknown values.
            throw new ArgumentException();
        }

        /// <summary>
        /// If name is invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if content parses as a double, the contents of the named
        /// cell becomes that double.
        /// 
        /// Otherwise, if content begins with the character '=', an attempt is made
        /// to parse the remainder of content into a Formula f using the Formula
        /// constructor.  There are then three possibilities:
        /// 
        ///   (1) If the remainder of content cannot be parsed into a Formula, a 
        ///       SpreadsheetUtilities.FormulaFormatException is thrown.
        ///       
        ///   (2) Otherwise, if changing the contents of the named cell to be f
        ///       would cause a circular dependency, a CircularException is thrown,
        ///       and no change is made to the spreadsheet.
        ///       
        ///   (3) Otherwise, the contents of the named cell becomes f.
        /// 
        /// Otherwise, the contents of the named cell becomes content.
        /// 
        /// If an exception is not thrown, the method returns a list consisting of
        /// name plus the names of all other cells whose value depends, directly
        /// or indirectly, on the named cell. The order of the list should be any
        /// order such that if cells are re-evaluated in that order, their dependencies 
        /// are satisfied by the time they are evaluated.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        /// <param name="name">The name of the cell getting set</param>
        /// <param name="content">The content that the named cell will hold</param>
        /// <returns>A list that will order all cells that will need to
        /// be re-evaluated when setting this cell</returns>
        public override IList<string> SetContentsOfCell(string name, string content)
        {
            //Check for name validity, and then set name to its normalized form
            name = checkNameThenNormalize(name);
            Changed = true;

            IList<string> cellsToReEvaluate;

            //Check to see what the content type is, and then use the right method.
            if (double.TryParse(content, out double d))
                cellsToReEvaluate = SetCellContents(name, d);
            //Formula's start with '='
            else if (content.StartsWith("="))
                cellsToReEvaluate = SetCellContents(name, new Formula(content.Substring(1), normalize, isValid));
            else
                cellsToReEvaluate = SetCellContents(name, content);

            //ReEvaluate all the cells that need to be. CURRENTLY CAUSE STACKOVERFLOW ERROR.
            foreach (string var in cellsToReEvaluate)
                if (cells.ContainsKey(var))
                    if (cells[var].contents is Formula f)
                        SetCell(var, f);
                    

            return cellsToReEvaluate;
        }

        /// <summary>
        /// Helper method to check if name is valid or not. If not throw a
        /// InvalidNameException, and if it is a valid name, do nothing.
        /// </summary>
        /// <param name="name">The name of the cell getting checked.</param>
        /// <exception cref="InvalidNameException">Throws if the name is invalid.</exception>
        private string checkNameThenNormalize(string name)
        {
            if (Regex.IsMatch(name, "^[a-zA-Z]*[0-9]*$") && isValid(name))
                return normalize(name);
            throw new InvalidNameException();
            
        }

        /// <summary>
        /// Will take in UID, and check if user exists using helper method above, if user doesn't exist will add user to given save file, 
        /// will enter the time the user logged in and then save the file.
        /// </summary>
        /// <returns>The name of the user logging in, or NOT FOUND if user still needs to sign user agreement</returns>
        public override string LoginUser(string ID, string logFilePath)
        {
            //The log might not exist yet in this folder, so a new log will be created based off of the date
            if (!logFilePath.Contains(DateTime.Today.ToString().Split(" ").First().Replace("/", "-")))
                logFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Log Files\log" + DateTime.Today.ToString().Split(" ").First().Replace("/", "-") + ".csv";
            Spreadsheet userLog = new();
            
            //First the file will attempt to open, and if it fails, then that means a new file needs to be created for this date.
            try
            {
                userLog = new Spreadsheet(logFilePath, s => true, s => s.ToUpper(), "lab");
            }
            catch
            {
                userLog.Save(logFilePath);
            }
       
            //The first char is a '0' and will become a u for their 'u'IDs
            ID = "u" + ID.Substring(1);

            //Check whether user exists or not
            string[] userInfo = new string[3];
            string cellName = "";
            int cellNum = 1;
            bool foundEmptyCell = false;
            if (userLog.cellValues.ContainsValue(ID))
            {
                //Find the next empty cell to the right of it (same number different letter) and log the current time.
                //Get the entry that has the value so we can find the key (cell name), and then get the number from the name ('1' from 'A1')
                cellName = userLog.cellValues.First(entryLog => entryLog.Value.Equals(ID)).Key;
                char cellLetter = cellName.First();
                cellNum = int.Parse(cellName.Substring(1));
                //Then with the cell number, the next empty cell to the right can be found
                while (cellLetter != 'Z')
                {
                    //Increment the letter and then see if the log file at that cell is empty or not.
                    cellLetter = (char)(cellLetter + 1);
                    if (!userLog.cells.ContainsKey(cellLetter + cellNum.ToString()))
                    {
                        cellName = cellLetter + cellNum.ToString();
                        foundEmptyCell = true;
                        break;
                    }
                }
                if (!foundEmptyCell)
                    throw new SpreadsheetReadWriteException("Log file full for current student, talk to a Lab Associate for help.");

                //Get this user's name from the log.
                userInfo[0] = (string)userLog.GetCellContents("B" + cellNum.ToString());
                userInfo[1] = (string)userLog.GetCellContents("C" + cellNum.ToString());
                userInfo[2] = (string)userLog.GetCellContents("D" + cellNum.ToString());
            }
            else
            {
                //Since the ID isn't in today's log yet, search for them in a saved file full of all registered students using private helper method
                userInfo = GetStudentInfo(ID);

                //If the student doesn't exist in the user/student list, then return false, and then the user will go through
                //the process of signing the user agreement and then after that they will have been added and this method will go again.
                if (userInfo[0].Equals("NOT FOUND"))
                    return "NOT FOUND";

                //Since user ID wasn't found in the log file, search for the next empty cell in the A column to start populating the row.
                cellNum = 1;
                while (!foundEmptyCell)
                {
                    if (!userLog.cells.ContainsKey("A" + cellNum))
                    {
                        cellName = "A" + cellNum;
                        foundEmptyCell = true;
                        break;

                    }
                    cellNum++;
                }
                //Now with this cell, the User's ID will be put into this first row.
                userLog.SetCellContents(cellName, ID);

                //The user's name will be input in the next two columns to the right.
                cellName = "B" + cellNum;
                userLog.SetContentsOfCell(cellName, userInfo[0]);
                cellName = "C" + cellNum;
                userLog.SetContentsOfCell(cellName, userInfo[1]);

                //The user's class will be added in the D col
                userLog.SetContentsOfCell("D" + cellNum, userInfo[2]);

                //Then the cell we need for the logging of the time will be the next cell to the right of the firstName and lastName boxes.
                cellName = "E" + cellNum;
            }

            //Now that the cellName has been found for an empty cell, the time will be logged
            userLog.SetContentsOfCell(cellName, DateTime.Now.ToShortTimeString());

            //Now that we've logged this user in, make sure the userLog is saved.
            userLog.Save(logFilePath);

            cellName = userLog.cellValues.First(entryLog => entryLog.Value.Equals(ID)).Key;
            cellNum = int.Parse(cellName.Substring(1));

            
            return userInfo[0] + " " + userInfo[1];
            
        }

        //This private method will search through a specified file that is full of all student ID's 
        //and if the student is registered into system, will return their first and last name
        private string[] GetStudentInfo(string ID)
        {
            string[] studentInfo = new string[3];
            studentInfo[0] = "NOT FOUND";
            studentInfo[1] = "NOT FOUND";
            studentInfo[2] = "NOT FOUND";
                
            //If the student is inside the ID file, then get their first and last name from the B and C cells.
            if (IDList != null && IDList.cellValues.ContainsValue(ID))
            {
                string cellName = IDList.cellValues.First(entryLog => entryLog.Value.Equals(ID)).Key;
                string cellNum = cellName.Substring(1);
                string[] temp = IDList.cellValues['B' + cellNum].ToString()!.Split(" ");
                //Check to see if the user entered their name in the format last, first, or first last
                if (temp[0].Contains(",")) {
                    studentInfo[0] = temp[1];
                    studentInfo[1] = temp[0].Replace(",", "");
                } else
                {
                    studentInfo[0] = temp[0];
                    studentInfo[1] = temp[1];
                }

                //Get the class from the IDList too
                studentInfo[2] = IDList.cellValues['C' + cellNum].ToString()!;
                    
            }
          
            return studentInfo;

            
        }

        /// <summary>
        /// Will attempt to save the spreadsheet filled with student ID information to the program to try to prevent the issue of 
        /// multiple processes trying to edit/view the same file at once - such as someone looking at the ID file while this program
        /// attempts to check it.
        /// </summary>
        /// <returns> 
        /// A bool showing whether or not the ID list file was successfully saved to the program or not.
        /// </returns>
        public override bool GetIDList()
        {
            try
            {
                IDList = new Spreadsheet(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Log Files\studentList.csv", s => true, s => s.ToUpper(), "lab");
            } 
            catch (SpreadsheetReadWriteException e)
            {
                if (e.Message.Equals("The file path was not able to be located."))
                {
                    //The File doesn't exist so lets create it.
                    IDList = new();

                    //There will be headers for the columns in the first row:
                    IDList.SetContentsOfCell("A1", "ID");
                    IDList.SetContentsOfCell("B1", "Name");
                    IDList.SetContentsOfCell("C1", "Class");
                    IDList.SetContentsOfCell("D1", "Time Signed");

                    IDList.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Log Files\studentList.csv");
                    return true;
                }
                //The file exists, but other errors are still happening, this most likely means it is open by another process.
                else
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Will save the user's given info they gave by signing the user agreement into the signed user file.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="name"></param>
        /// <param name="theClass"></param>
        public override void AddUsersInformation(string ID, string name, string theClass)
        {
            if (IDList == null)
                GetIDList();

            //The first char is a '0' and will become a u for their 'u'IDs
            ID = "u" + ID.Substring(1);

            //Check if the user has already been saved, if so just return, they've already signed agreement.
            if (IDList!.cellValues.ContainsKey(ID))
                return;

            //User isn't in file, the next empty row will be found.
            bool foundEmptyCell = false;
            int cellNum = 1;
            while (!foundEmptyCell)
            {
                if (!IDList.cells.ContainsKey("A" + cellNum))
                    break;
                
                cellNum++;
            }

            //User will be added now. A col will be ID, B col will be the name of the user, C col will be class they are in, D will be time of signature.
            IDList.SetContentsOfCell("A" + cellNum, ID);
            IDList.SetContentsOfCell("B" + cellNum, name);
            IDList.SetContentsOfCell("C" + cellNum, theClass);
            IDList.SetContentsOfCell("D" + cellNum, DateTime.Now.ToString());

            //Save the file
            IDList.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Log Files\studentList.csv");

        }
    }

    /// <summary>
    /// A class that represents a cell, with a valid name, and a given value
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Cell
    {
        //The name of the cell should never change besides from getting set, so it should be readonly
        public readonly string name;
        
        
        //The contents should be able to be fully accessed and changed frequently so it can be public.
        public object contents;

        //The contents in string form.
        private string strForm = "";

        [JsonProperty]
        public string stringForm { get => strForm;  protected set => strForm = value;  }
        
        public Cell(string name, object value)
        {
            this.name = name;
            this.contents = value;
            if (value is Formula f)
                this.stringForm = " = " + f.ToString();
            else if (value is double d)
                this.stringForm = d.ToString();
            else  
                this.stringForm = (string)value;
        }

    }
}
