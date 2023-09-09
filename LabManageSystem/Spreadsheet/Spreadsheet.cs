using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpreadsheetUtilities;
using Newtonsoft.Json;
using SS;

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
                sprdsht = JsonConvert.DeserializeObject<Spreadsheet>(File.ReadAllText(filePath));
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
            

            if (sprdsht is not null)
            {
                //Check if the version is the same
                if (this.version != sprdsht.Version)
                    throw new SpreadsheetReadWriteException("The Versions of the Spreadsheet software are not the same.");
                //Check that for each cell, the name is valid and there are no circular exceptions.
                foreach (KeyValuePair<string, Cell> cell in sprdsht.cells)
                {
                    try
                    {
                        this.SetContentsOfCell(cell.Key, cell.Value.stringForm);
                    }
                    catch (InvalidNameException)
                    {
                        throw new SpreadsheetReadWriteException("There are invalid variable names in the file.");
                    }
                    catch (CircularException)
                    {
                        throw new SpreadsheetReadWriteException("The loaded spreadsheet file's cells has a circular exception.");
                    }
                    catch (FormulaFormatException)
                    {
                        throw new SpreadsheetReadWriteException("Some of the given formulas are not valid formulas.");
                    }


                }
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
                    
                //Also check if the cell is changing from something that could be a double to something else.
                else if (!(data is double || data is Formula) && cellValues.ContainsKey(name))
                {
                    cellValues.Remove(name);
                    dg.ReplaceDependees(name, new List<string>(){""});
                }
                    
                return;
            }
                
            //Then if data is not an empty string, create a new cell and add it to the cells.
            else if (data.ToString() != "")
            {
                cells.Add(name, new Cell(name, data));
                //Check if data can be evaluated down or is a double, and is not already in cellValues
                if ((data is double || data is Formula) && !cellValues.ContainsKey(name))
                {
                    if (data is Formula f)
                        cellValues.Add(name, f.Evaluate(lookup));
                        
                    else cellValues.Add(name, data);
                }
                    
                
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
                File.WriteAllText(filename, JsonConvert.SerializeObject(this));
            }
            catch (DirectoryNotFoundException)
            {
                throw new SpreadsheetReadWriteException("The path the file was trying to save to does not exist.");
            }
            catch (IOException)
            {
                throw new SpreadsheetReadWriteException("The given filepath will not work.");
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
                //Then check to see if it has a value that has been evaluated
                if (cellValues.ContainsKey(name))
                    return cellValues[name];
                //If not an evaluated value, then it is a string
                return cells[name].contents;
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
        /// Will check if UID is already inside of a given spreadsheet save
        /// </summary>
        protected override bool CheckIfUserExists(int ID, Spreadsheet userLog)
        {
            if (userLog.cellValues.ContainsValue((double)ID))
                return true;
            return false;
        }

        /// <summary>
        /// Will take in UID, and check if user exists using helper method above, if user doesn't exist will add user to given save file, 
        /// will enter the time the user logged in and then save the file.
        /// </summary>
        public override void LoginUser(int ID, string logFilePath)
        {
            //Try to load the log sheet
            Spreadsheet userLog = new Spreadsheet(logFilePath, s => true, s => s.ToUpper(), "lab");

            //Check whether user exists or not
            string cellName = "";
            bool foundEmptyCell = false;
            if (CheckIfUserExists(ID, userLog))
            {
                //Since the User already exists, find the next empty cell to the right of it (same number different letter) and log the current time.
                //Get the entry that has the value so we can find the key (cell name), and then get the number from the name ('1' from 'A1')
                cellName = userLog.cellValues.First(logEntry => String.Equals(logEntry.Value, (double)ID)).Key;
                char cellLetter = cellName.First();
                string cellNum = cellName.Substring(1);
                //Then with the cell number, the next empty cell to the right can be found
                while(cellLetter != 'Z')
                {
                    //Increment the letter and then see if the log file at that cell is empty or not.
                    cellLetter = (char)(cellLetter + 1);
                    if (!userLog.cells.ContainsKey(cellLetter + cellNum)) {
                        cellName = cellLetter + cellNum;
                        foundEmptyCell = true;
                        break;
                    }
                }
                if (!foundEmptyCell)
                    throw new SpreadsheetReadWriteException("Log file full for current student, talk to a Lab Associate for help.");
            }
            else
            {
                //Since user ID wasn't found in the log file, search for the next empty cell in the A column to start populating the row.
                int cellNum = 1;
                while (cellNum <= 100)
                {
                    if (!userLog.cells.ContainsKey("A" + cellNum))
                    {
                        cellName = "A" + cellNum;
                        foundEmptyCell=true;
                        break;
                    }
                    cellNum++;
                }
                if (!foundEmptyCell)
                    throw new SpreadsheetReadWriteException("Log file has no more room for students. Talk to a Lab Associate for help.");
                //Now with this cell, the User's ID will be put into this first row.
                userLog.SetContentsOfCell(cellName, ID.ToString());
                //Then the cell we need for the logging of the time will be the next cell to the right.
                cellName = "B" + cellNum;
            }
            //Now that the cellName has been found for an empty cell, the time will be logged
            userLog.SetContentsOfCell(cellName, DateTime.Now.ToString());

            //Now that we've logged this user in, make sure the userLog is saved.
            userLog.Save(logFilePath);
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
                this.stringForm = "=" + f.ToString();
            else if (value is double d)
                this.stringForm = d.ToString();
            else  
                this.stringForm = (string)value;
        }

    }
}
