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
using Microcharts;
using System.Drawing;
using System.Diagnostics;
using SkiaSharp;
using WinRT;
using Windows.ApplicationModel.Email;
using Windows.Devices.Printers;
using Microsoft.UI.Xaml;
using Windows.Media.AppBroadcasting;
using Microsoft.UI.Xaml.Controls;
using Sett;

namespace SS
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Spreadsheet : AbstractSpreadsheet
    {
        [JsonProperty]
        private Dictionary<string, Cell> cells = new();
        
        private Dictionary<string, object> cellValues = new();
        private int numberOfRows = 0;
        
        private DependencyGraph dg = new ();
        private Func<string, bool> isValid;
        private Func<string, string> normalize;
        private string version;
        private bool change;
        private Spreadsheet? IDList;
        public Spreadsheet? CurrentlyLoggedIn;
        public Settings Settings = new Settings();
        

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
                        numberOfRows++;
                        
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
                    List<int> keysAsNums = new();
                    //Move the letter to the other side: A1 -> 1A, A10 -> 10A, and convert to number format.
                    for (int i = 0; i < sortedKeys.Count; i++)
                    {
                        sortedKeys[i] = new string(sortedKeys[i].Substring(1) + (int)sortedKeys[i].First());
                        keysAsNums.Add(int.Parse(sortedKeys[i]));
                    }
                    //Sort and then change back, now the keys used below will be grabbed by col-row order.
                    keysAsNums.Sort();
                    for (int i = 0; i < sortedKeys.Count; i++)
                    {
                        char letter = (char)int.Parse(keysAsNums[i].ToString()[^2..]);
                        sortedKeys[i] = new string(letter + keysAsNums[i].ToString()[..^2]);
                    }

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
        /// will enter the time the user logged in and then save the file. Will return the name of the user.
        /// Updates the CurrentOccupancyGrid on the HomePage to show who is currently logged in.
        /// </summary>
        /// <returns>The name of the user logging in, or NOT FOUND if user still needs to sign user agreement</returns>
        public override string LoginUser(string ID, List<string> hiddenInfoFields)
        {
            string logFilePath = Settings.saveFileLocation + "Logs\\" + DateTime.Now.ToString("yyyy-MMMM") + "\\log" + DateTime.Today.ToString().Split(" ").First().Replace("/", "-") + ".csv";
            Spreadsheet userLog = new();
            LoadSettings();
            
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
            List<string> userInfo = new List<string>();
            string cellName = "";
            int cellNum = 1;
            bool foundEmptyCell = false;
            //These next vals will be used for the CurrentlyLoggedIn Spreadsheet.
            bool SomeoneIsLoggingOut = false;
            string cName = "";
            int cNum = 1;

            if (userLog.cellValues.ContainsValue(ID))
            {
                //Find the next empty cell to the right of it (same number different letter) and log the current time.
                //Get the entry that has the value so we can find the key (cell name), and then get the number from the name ('1' from 'A1')
                cellName = userLog.cellValues.First(entryLog => entryLog.Value.Equals(ID)).Key;
                char cellLetter = cellName.First();
                cellNum = int.Parse(cellName.Substring(1));
                int logCount = 0; //Check how many times the user has used this method, if its even this is a new log in (they've already logged in and then logged out in a pair), odd means they are logging out.

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
                    if (DateTime.TryParse((string)userLog.cellValues[cellLetter + cellNum.ToString()], out DateTime res))
                        logCount++;
                }

                if (!foundEmptyCell)
                    throw new SpreadsheetReadWriteException("Log file full for current student, talk to a Lab Associate for help.");

                //Get this user's information 
                userInfo = GetStudentInfo(ID);

                if (logCount % 2 == 1)
                    SomeoneIsLoggingOut = true;
                else SomeoneIsLoggingOut = false;
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

                //Then custom info fields will be placed after.
                char cL = 'C';
                for (int i = 2; i < userInfo.Count; i++)
                {
                    cL = (char)(cL + 1);
                    userLog.SetContentsOfCell(cL + cellNum.ToString(), userInfo[i]);
                }

                cL = (char)(cL + 1);
                //Then the cell we need for the logging of the time will be the next cell to the right of the last info field.
                cellName = cL + cellNum.ToString();
                SomeoneIsLoggingOut = false;
            }

            //Now that the cellName has been found for an empty cell, the time will be logged
            userLog.SetContentsOfCell(cellName, DateTime.Now.ToShortTimeString());

            //Now that we've logged this user in, make sure the userLog is saved.
            userLog.Save(logFilePath);

            //Update CurrentlyLoggedIn depending on if this attempt was a log in or a log out.
            if (SomeoneIsLoggingOut)
            {
                try
                {
                    cName = CurrentlyLoggedIn!.cellValues.First(entryLog => entryLog.Value.Equals(ID)).Key;
                    char cLetter = cName.First();
                    cNum = int.Parse(cName.Substring(1));
                    //The last cell to delete will be the Time Logged In Cell, which should always exist.
                    string endCell = CurrentlyLoggedIn!.cellValues.First(entryLog => entryLog.Value.Equals("Time Logged In:")).Key;
                    char endCol = endCell.First();

                    while (cLetter != endCol) //Empty the fields on the CurrentlyLoggedIn Sheet for this user.
                    {
                        CurrentlyLoggedIn!.SetContentsOfCell(cName, "");
                        cLetter = (char)(cLetter + 1);
                        cName = cLetter + cNum.ToString();
                    }

                    //Then also delete that last col's field too.
                    CurrentlyLoggedIn!.SetContentsOfCell(endCol + cNum.ToString(), "");
                }
                catch
                {
                    //User was logged in but CurrentlyLoggedIn got reset somewhere earlier, so just log them out without deleting anything - since the info is already gone on the currentlyLoggedIn anyways
                }
            }
            else
            {
                //Find next empty line in CurrentlyLoggedIn Spreadsheet.
                cNum = 2;
                while (true)
                {
                    if (!CurrentlyLoggedIn!.cells.ContainsKey("A" + cNum))
                    {
                        cName = "A" + cNum;
                        break;
                    }
                    cNum++;
                }

                CurrentlyLoggedIn!.SetCellContents("A" + cNum, ID);
                CurrentlyLoggedIn!.SetContentsOfCell("B" + cNum, userInfo[0]);
                CurrentlyLoggedIn!.SetContentsOfCell("C" + cNum, userInfo[1]);

                //With custom info fields, the column headers of the currently logged in sheet matter, so we can tell what to display here or not.
                for (int i = 2; i < userInfo.Count; i++)
                {
                    string IDLogColHeader = IDList!.cellValues.First(entryLog => entryLog.Value.Equals(userInfo[i]) && IDList!.cellValues["A" + entryLog.Key[1..]].Equals(ID)).Key;
                    IDLogColHeader = IDList!.cellValues[IDLogColHeader.First() + "1"].ToString()!;
                    if (hiddenInfoFields.Contains(IDLogColHeader.Trim().Trim(':') + ": "))
                        continue;

                    //Check to see if Currently Logged In will show this info field, if so then we can just place the field found into Currently Logged in.
                    try
                    {
                        //Since CurrentlyLoggedIn will have the col headers include a ': ', confirm that the IDLogColHeader is same format
                        IDLogColHeader = IDLogColHeader.Trim().Trim(':') + ": "; //Trim it in case it already has ': ', and then add it (since sometimes it won't)
                        string field = CurrentlyLoggedIn!.cellValues.First(entryLog => entryLog.Value.Equals(IDLogColHeader)).Key;
                        CurrentlyLoggedIn!.SetContentsOfCell(field.First() + cNum.ToString(), userInfo[i]); //With just Field.First() we know which column to place this.
                    }
                    catch { } //If that field does not exist inside CurrentlyLoggedIn, then it's either hidden, or something got missed, but just continue
                }
                string timeField = CurrentlyLoggedIn!.cellValues.First(entryLog => entryLog.Value.Equals("Time Logged In:")).Key;
                CurrentlyLoggedIn!.SetContentsOfCell(timeField.First() + cNum.ToString(), DateTime.Now.ToShortTimeString());
            }
            
            CurrentlyLoggedIn!.Save(Settings.saveFileLocation + "currentlyLoggedIn.csv");

            if (SomeoneIsLoggingOut) 
                return userInfo[0] + " " + userInfo[1] + " Logged out: ";
            return userInfo[0] + " " + userInfo[1] + " Logged in: ";
        }

        //This method will search through a specified file that is full of all student ID's 
        //and if the student is registered into system, will return their information.
        public List<string> GetStudentInfo(string ID)
        {
            List<string> studentInfo = new List<string> { "NOT FOUND", "NOT FOUND" };

            //If the student is inside the ID file, then get their info fields
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
                    try
                    {
                        studentInfo[1] = temp[1];
                    }
                    catch
                    {
                        //If this error occurs, then User only input a first name, so just leave the last name part blank
                        studentInfo[1] = "";
                    }
                }

                //Get remaining fields (besides for time signed)
                char cellLetter = 'B';
                while(true)
                {
                    cellLetter = (char)(cellLetter + 1);
                    if (!IDList.cellValues.ContainsKey(cellLetter + cellNum) || DateTime.TryParse(IDList.cellValues[cellLetter + cellNum].ToString(), out DateTime r))
                        break;
                    studentInfo.Add(IDList.cellValues[cellLetter + cellNum].ToString()!);
                }
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
        public override bool GetIDList(List<string> infoFields)
        {
            try
            {
                IDList = new Spreadsheet(Settings.saveFileLocation + "userList.csv", s => true, s => s.ToUpper(), "lab");
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

                    //There will be headers for the columns in the first row:
                    char cellLetter = 'B';
                    for (int i = 0; i < infoFields.Count; i++)
                    {
                        cellLetter = (char)(cellLetter + 1);
                        IDList.SetContentsOfCell(cellLetter + "1", infoFields[i]);
                    }
                    cellLetter = (char)(cellLetter + 1);
                    IDList.SetContentsOfCell(cellLetter + "1", "Time Signed:");

                    IDList.Save(Settings.saveFileLocation + "userList.csv");
                    return true;
                }
                //The file exists, but other errors are still happening, this most likely means it is open by another process.
                else
                    return false;
            }

            return true;
        }

        /// <summary>
        /// When user customizes a specific into field in the ID list, this method can be called to edit the field.
        /// </summary>
        public override void EditIDListField(String old, String newName, String newField, String fieldToDelete)
        {
            IDList ??= new Spreadsheet(Settings.saveFileLocation + "userList.csv", s => true, s => s.ToUpper(), "lab");

            if (newField is not null) //Instead of changing an existing IDList field, a new Field is getting added
            {
                //The Time Signed: column will be moved on the ID list over, and this new field column will be input.
                string cellName = IDList.cellValues.First(entryLog => entryLog.Value.Equals("Time Signed:")).Key;
                char cL = cellName.First();
                int cNum = int.Parse(cellName.Substring(1));

                //First, the Time Signed Col will be moved over:
                while(IDList.cellValues.ContainsKey(cL + cNum.ToString()))
                {
                    string temp = IDList.cellValues[cL + cNum.ToString()].ToString()!;
                    IDList.SetContentsOfCell(cL + cNum.ToString(), ""); //Delete old location
                    IDList.SetContentsOfCell((char)(cL + 1) + cNum.ToString(), temp);
                    //Increment row
                    cNum++;
                }

                //Next, place new Info Field in
                IDList.SetContentsOfCell(cellName, newField);

                IDList.Save(Settings.saveFileLocation + "userList.csv");
                return;
            }

            if (fieldToDelete is not null) //A field is getting removed.
            {
                //Field To Delete col will be erased
                string cellName = IDList.cellValues.First(entryLog => entryLog.Value.Equals(fieldToDelete)).Key;
                char cL = cellName.First();
                int cNum = int.Parse(cellName.Substring(1));

                while (IDList.cellValues.ContainsKey(cL + cNum.ToString()))
                {
                    IDList.SetContentsOfCell(cL + cNum.ToString(), ""); //Delete 
                    //Increment row
                    cNum++;
                }

                //Move rows to right over left by one to fill in 
                cL = (char)(cL + 1);
                cNum = 1;
                while (IDList.cellValues.ContainsKey(cL + cNum.ToString()))
                {
                    while (IDList.cellValues.ContainsKey(cL + cNum.ToString()))
                    {
                        string temp = IDList.cellValues[cL + cNum.ToString()].ToString()!;
                        IDList.SetContentsOfCell(cL + cNum.ToString(), ""); //Delete old location
                        IDList.SetContentsOfCell((char)(cL - 1) + cNum.ToString(), temp); 
                        //Increment row
                        cNum++;
                    }
                    cL = (char)(cL + 1);
                    cNum = 1;
                }

                IDList.Save(Settings.saveFileLocation + "userList.csv");
                return;
            }

            if (!IDList!.cellValues.ContainsValue(old)) //Somehow the field they are trying to change isn't detected inside the ID list.
                return;
            char cellLetter = 'C';
            while(true)
            {
                if (IDList.cellValues[cellLetter + "1"].Equals(old))
                {
                    IDList.SetContentsOfCell(cellLetter + "1", newName); break; //Since we already checked that the spreadsheet holds the field, the loop will break.
                }
                cellLetter = (char)(cellLetter + 1);
            }
            IDList.Save(Settings.saveFileLocation + "userList.csv");
        }

        /// <summary>
        /// Loads a spreadsheet (or creates a new spreadsheet if not yet made) for the purpose of keeping track of who is currently logged in.
        /// </summary>
        public override Spreadsheet GetCurrentlyLoggedInSpreadsheet(List<String> VisibleFieldsList)
        {
            //Possible that CurrentlyLoggedIn already exists, but will be reset daily once software boots.
            CurrentlyLoggedIn = new();

            //There will be headers for the columns in the first row:
            CurrentlyLoggedIn.SetContentsOfCell("A1", "ID:");
            CurrentlyLoggedIn.SetContentsOfCell("B1", "First Name:");
            CurrentlyLoggedIn.SetContentsOfCell("C1", "Last Name:");
            bool endReached = false;
            char cellLetter = 'C';
            char CurrLogInCellLetter = 'C';
            while (true)
            {
                if (endReached || VisibleFieldsList.Count == 0)
                    break;
                for (int i = 0; i <  VisibleFieldsList.Count; i++)
                {
                    try
                    {
                        
                        if (IDList!.cells.ContainsKey(cellLetter + "1"))
                        {
                            string val = (string)IDList!.cellValues[cellLetter + "1"];
                            val = val.Trim();
                            val = val.Trim(':');
                            string field = VisibleFieldsList[i].ToString();
                            field = field.Trim();
                            field = field.Trim(':');
                            if (val.Equals("Time Signed") || field.Equals(val)) //If they match or there is still a visible field to be shown, but the val from the IDList is Time Signed, software will insert info field and move Time signed to next slot.
                            {
                                CurrLogInCellLetter = (char)(CurrLogInCellLetter + 1);
                                CurrentlyLoggedIn.SetContentsOfCell(CurrLogInCellLetter + "1", VisibleFieldsList[i]);
                                if (i == VisibleFieldsList.Count - 1)
                                {
                                    endReached = true; break;
                                }
                            }
                            
                        }
                        else
                        {
                            endReached = true;
                            break;
                        }

                        cellLetter = (char)(cellLetter + 1);
                    }
                    catch //In case an exception occurs here (maybe due to User changing Info Fields often or something similar) prevent crashing and just end loop.
                    {
                        //Instead, a compromise can occur here where if a user doesn't have specific fields causing the crash, this spot can be empty.
                        CurrLogInCellLetter = (char)(CurrLogInCellLetter + 1);
                        endReached = true; break;
                    }
                    
                }
            }
            CurrLogInCellLetter = (char)(CurrLogInCellLetter + 1);
            CurrentlyLoggedIn.SetContentsOfCell(CurrLogInCellLetter + "1", "Time Logged In:");
            CurrentlyLoggedIn.Save(Settings.saveFileLocation + "currentlyLoggedIn.csv");
            return CurrentlyLoggedIn;
        }

        /// <summary>
        /// User will be automatically logged out after 2 hours, as in their username and info will no longer be shown on the currently logged in sheet.
        /// </summary>
        public override void LogoutUserFromCurrentlyLoggedInSheet(string ID)
        {
            //Getting file
            string logFilePath = Settings.saveFileLocation + "Logs\\" + DateTime.Now.ToString("yyyy -MMMM") + "\\log" + DateTime.Today.ToString().Split(" ").First().Replace("/", "-") + ".csv";
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

            //First check if the user already logged out manually, and if so just return.
            if (!userLog.cellValues.ContainsValue(ID))
                return;

            //User will be logged out automatically
            try
            {
                string cName = CurrentlyLoggedIn!.cellValues.First(entryLog => entryLog.Value.Equals(ID)).Key;
                char cLetter = cName.First();
                int cNum = int.Parse(cName.Substring(1));
                //The last cell to delete will be the Time Logged In Cell, which should always exist.
                string endCell = CurrentlyLoggedIn!.cellValues.First(entryLog => entryLog.Value.Equals("Time Logged In:")).Key;
                char endCol = endCell.First();

                while (cLetter != endCol) //Empty the fields on the CurrentlyLoggedIn Sheet for this user.
                {
                    CurrentlyLoggedIn!.SetContentsOfCell(cName, "");
                    cLetter = (char)(cLetter + 1);
                    cName = cLetter + cNum.ToString();
                }

                //Then also delete that last col's field too.
                CurrentlyLoggedIn!.SetContentsOfCell(endCol + cNum.ToString(), "");
            }
            catch
            {
                //User was logged in but CurrentlyLoggedIn got reset somewhere earlier, so just log them out without deleting anything - since the info is already gone on the currentlyLoggedIn anyways
            }

            CurrentlyLoggedIn!.Save(Settings.saveFileLocation + "currentlyLoggedIn.csv");


        }

        /// <summary>
        /// Will save the user's given info they gave by signing the user agreement into the signed user file.
        /// </summary>
        public override void AddUsersInformation(List<string> userInfo)
        {
            if (IDList is null) //If someone is logging in, settings are already established, and the ID list should have been created
                IDList = new Spreadsheet(Settings.saveFileLocation + "userList.csv", s => true, s => s.ToUpper(), "lab");

            //First item in userInfo will be ID. The first char is a '0' and will become a u for their 'u'IDs
            string ID = "u" + userInfo[0].Substring(1);

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

            //User will be added now. A col will be ID, B col will be the name of the user, Then next cols will hold user specified fields, Last col will be time of signature.
            IDList.SetContentsOfCell("A" + cellNum, ID);
            IDList.SetContentsOfCell("B" + cellNum, userInfo[1]);
            for (int i = 2; i < userInfo.Count; i++)
            {
                //Check info given compared to the column headers of the userID file to place information where it should be.
                string fieldHeader = userInfo[i].Split("&&")[0].Trim().Trim(':');
                string fieldInfo = userInfo[i].Split("&&")[1];
                string IDLogColHeader = IDList!.cellValues.First(entryLog => entryLog.Value.Equals(fieldHeader)).Key;
                IDList!.SetContentsOfCell(IDLogColHeader.First() + cellNum.ToString(), fieldInfo);
            }
            string lastCell = IDList!.cellValues.First(entryLog => entryLog.Value.Equals("Time Signed:")).Key; //Enter the time that the User Signed
            IDList.SetContentsOfCell(lastCell.First() + cellNum.ToString(), DateTime.Now.ToString());

            //Save the file
            IDList.Save(Settings.saveFileLocation + "userList.csv");
        }

        /// <summary>
        /// Will load the settings object from the given filepath so that the spreadsheet object has access to settings fields.
        /// </summary>
        public override void LoadSettings()
        {
            Settings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        }

        /// <summary>
        /// Will gather statistics of lab traffic between the time duration of from and to, creating a ChartEntry list 
        /// that will correspond to average amount of people on the different days of the week, or on the different hours of the day - corresponding to a mode of 0 to 4 for Monday thru Friday or 5 for each week day.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public override List<ChartEntry> GatherStatistics(String from, String to, int mode)
        {
            string currLogToCheck = "";
            Spreadsheet dayToCheck;
            DayOfWeek specificDay = DayOfWeek.Monday;
            List<ChartEntry> entries = new List<ChartEntry>();
            DateTime fromDate = DateTime.Parse(from).Date;
            DateTime toDate = DateTime.Parse(to).Date;
            List<int> avgs = new List<int>();
            List<int> counts = new List<int>(); //counts[0] = 3 means 3 sundays were included in mode of 7 (all days) or 3 means 3 8:00 AMs for other modes, will be used for finding average.
            DateTime dateGettingChecked = fromDate;
            List<Color> colors = new List<Color>() { Color.Blue, Color.Plum, Color.Green, Color.Red, Color.Yellow, Color.Turquoise, Color.Orange, Color.AliceBlue, Color.BlanchedAlmond, Color.Crimson, Color.Gold, Color.Fuchsia };
            switch(mode)
            {
                case 0:
                    specificDay = DayOfWeek.Monday;
                    break;
                case 1:
                    specificDay = DayOfWeek.Tuesday;
                    break;
                case 2:
                    specificDay = DayOfWeek.Wednesday;
                    break;
                case 3:
                    specificDay = DayOfWeek.Thursday;
                    break;
                case 4:
                    specificDay = DayOfWeek.Friday;
                    break;
                case 5:
                    for (int i = 0; i < 7; i++)
                    {
                        avgs.Add(0);
                        counts.Add(0);
                    }
                    break;
            }

            if (mode != 5)
            {
                for (int i = 0; i < 12; i++) //0 represents 8 AM, 11 represents 7 PM
                {
                    avgs.Add(0);
                    counts.Add(0);
                }
            }
            
            int year = fromDate.Year;
            int month = fromDate.Month;
            int day = fromDate.Day;

            for (; year <= toDate.Year; year++)
            {
                for (; month <= toDate.Month; month++)
                {
                    int tillDay = DateTime.DaysInMonth(year, month);
                    if (month == toDate.Month)
                        tillDay = toDate.Day;
                    for (; day <= tillDay; day++)
                    {
                        dateGettingChecked = new DateTime(year, month, day);   
                        if (mode == 5) { counts[(int)dateGettingChecked.DayOfWeek]++; } //Will want the total number of mondays, tuesdays, etc, even if no one logged in on those days for computing averages. Mode for all weekdays specifically
                        //Attempt to load the log from the current dayToCheck, continue if it doesn't exist.
                        currLogToCheck = Settings.saveFileLocation + "Logs\\" + new DateTime(year, month, day).ToString("yyyy -MMMM") + "\\log" + dateGettingChecked.Date.ToString().Split(" ").First().Replace('/', '-') + ".csv";
                        try { dayToCheck = new Spreadsheet(currLogToCheck, s => true, s => s.ToUpper(), "lab"); }
                        catch { continue; }
                        if (mode == 5) //Mode for all days of week
                        { avgs[(int)dateGettingChecked.DayOfWeek] += dayToCheck.numberOfRows; }
                        else //Mode for a specific day, (Mode != 5)
                        {
                            if (dateGettingChecked.DayOfWeek.Equals(specificDay))
                                SpecificDayOfWeekStatsHelper(dayToCheck, ref avgs, ref counts); 
                        } 
                    }
                    day = 1;
                }
                month = 1;
            }
            
            for (int i = 0; i < avgs.Count; i++)
            {
                string colorsHex = "#" + colors[i].R.ToString("X2") + colors[i].G.ToString("X2") + colors[i].B.ToString("X2");
                if (avgs[i] != 0)
                    if (mode == 5)
                        entries.Add(new ChartEntry((float)avgs[i] / (float)counts[i]) { Label = Enum.GetName(typeof(DayOfWeek), i), ValueLabel = ((float)avgs[i] / (float)counts[i]).ToString(), Color = SKColor.Parse(colorsHex)});
                    else
                    { 
                        if (i + 8 < 12) //For labels of 8 AM to 11 AM
                            entries.Add(new ChartEntry((float)avgs[i] / (float)counts[i]) { Label = (i + 8) + "AM", ValueLabel = ((float)avgs[i] / (float)counts[i]).ToString(), Color = SKColor.Parse(colorsHex) });
                        else if (i + 8 == 12) //For label of 12 PM
                            entries.Add(new ChartEntry((float)avgs[i] / (float)counts[i]) { Label = 12 + "PM", ValueLabel = ((float)avgs[i] / (float)counts[i]).ToString(), Color = SKColor.Parse(colorsHex) });
                        else //For remaining labels of 1 PM to 7 PM, ex: i = 11 for 7 so i - 4 = 7
                            entries.Add(new ChartEntry((float)avgs[i] / (float)counts[i]) { Label = (i - 4) + "PM", ValueLabel = ((float)avgs[i] / (float)counts[i]).ToString(), Color = SKColor.Parse(colorsHex) });
                    }
            }

            return entries;
        }

        /// <summary>
        /// Helper method to help figure out how many people logged in per hour from 8 AM to 7 PM
        /// </summary>
        /// <param name="dayToCheck"></param>
        /// <param name="avgs"></param>
        /// <param name="count"></param>
        private static void SpecificDayOfWeekStatsHelper(Spreadsheet dayToCheck, ref List<int> avgs, ref List<int> counts)
        {
            int row = 1;
            char cellLetter;
            int cellNum;
            List<DateTime> times;

            while (dayToCheck.cells.ContainsKey("E" + row)) //Cells in E column are the ones getting set with the time.
            {

                times = new List<DateTime>();
                string cellName = "E" + row++;
                cellLetter = cellName.First();
                cellNum = int.Parse(cellName.Substring(1));
                //For each hour within the time the user has logged in, update stats. This loop gets the time Users are logged in for.
                while (dayToCheck.cells.ContainsKey(cellLetter + cellNum.ToString()))
                {

                    object d = dayToCheck.cellValues[cellLetter + cellNum.ToString()];
                    if (DateTime.TryParse(d.ToString(), out DateTime time) == false)
                        continue;
                    times.Add(time);
                    cellLetter = (char)(cellLetter + 1);
                }

                if (times.Count == 0)
                    continue;
                
                for (int i = 0; i < times.Count; i+=2)
                {
                    int logInTime = times[i].Hour;
                    int logOutTime = logInTime + 2; //If user forgot to log out manually, software will consider them as having logged out after staying 2 hours --> a guess at the average amount of time people usually stay in lab.
                    if (i + 1 < times.Count)
                        logOutTime = times[i + 1].Hour;

                    while (logInTime != logOutTime + 1 && logInTime != 8)
                    {
                        if (logInTime - 8 >= 0 && logInTime - 8 <= 11) //Check for people logging in before 8 AM or after 7 PM
                            avgs[logInTime - 8]++; //Minus 8 since time.Hour will give hour of day from 0 to 23, so 12 AM = 0, 8 AM = 8 - 8 = 0 --> avgs[0] will represent 8 AM.
                        else if (logInTime - 8 < 0)
                            avgs[0]++; //Consider people that logged in at 7 to 8 AM to just have logged in at 8, since Lab technically opens at 8
                        else
                            avgs[11]++; //People logging in after 7 will just always be considered to log in at 7. For weird cases of people logging in at 8 PM or later.
                        logInTime++;
                    }
                }
            }
            //Increment count list from 0 (8 AM) to 11 (7PM) each by one. These values will be used to find averages later, so we want to know the total number of 8 AM hours there are.
            //Example: Today and Yesterday someone logged in at 8AM ish so getting stats, total people found is 2, but this happened over two days, so on average, 2 people found / 2 8AMs = just 1 person is logged in from 8 to 9.
            for (int i = 0; i < 12; i++)
                counts[i]++;
            
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
