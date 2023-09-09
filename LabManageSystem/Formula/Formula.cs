// Skeleton written by Profs Zachary, Kopta and Martin for CS 3500
// Read the entire skeleton carefully and completely before you
// do anything else!

// Change log:
// Last updated: 9/8, updated for non-nullable types

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax (without unary preceeding '-' or '+'); 
    /// variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {
        //List to hold all valid and normalized variables.
        private List<string> variables;
        //List to hold all tokens in Formula, and the normalized variables.
        private List<string> tokenList;
        //A private field to hold the given isValid, normalize methods passed through in the ctor, or default ones.
        private Func<string, bool> isValid;
        private Func<string, string> normalize;

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Formula(String formula) :
            this(formula, s => s, s => true)
        {
        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            //Keep track of the passed in normalize delegate to use when evaluating vars.
            this.isValid = isValid;
            this.normalize = normalize;

            //Initizalize variables, and tokenList lists.
            variables = new List<string>();
            tokenList = new List<string>();

            //tokenCount will mainly be used to see if formula has at least one token.
            int tokenCount = 0;
            //parBalance will work by going positive when there are too many (, and negative when too many )
            //So if parBalance ends as 0, then there is an equal amount of parantheses.
            int parBalance = 0;

            //This bool will be used to see if any token follows after a (
            bool followingOpenPar = false;
            //This bool will see if a token follows after a number, variable, or )
            bool extraFollowing = false;
            //This bool will check to see if an operator follows after an operator
            bool followingOperator = false;

            IEnumerable<string> tokens = GetTokens(formula);
            foreach (string s in tokens)
            {
                //If s is not a decimal real number, or an acceptable operator, or variable, then the formula is syntactically incorrect.
                if (s != "(" && s != ")" && s != "+" && s != "-" && s != "*" && s != "/" && !double.TryParse(s, out double d) && !isValid(normalize(s)))
                    throw new FormulaFormatException("Formula is Syntactically incorrect.");

                tokenCount++;
                
                //If s is the token after a (, check to see if it is a variable, number, or (, if not throw exception.
                if (followingOpenPar && (s == ")" || s == "+" || s == "-" || s == "*" || s == "/") && (s != "(" || !isValid(normalize(s)) || !double.TryParse(s, out double i)))
                    throw new FormulaFormatException("Any token that immediately follows an opening parenthesis or an operator must be either a number, a variable, or an opening parenthesis.");
                else
                    //If s follows a ( and is still syntactically correct, set followingOpenPar to false.
                    followingOpenPar = false;

                //Check to see if an operator follows directly after an operator.
                if (followingOperator && (s == "+" || s == "-" || s == "*" || s == "/"))
                    throw new FormulaFormatException("Double Operators.");
                else
                    followingOperator = false;

                if (s == "+" || s == "-" || s == "*" || s == "/")
                    followingOperator = true;

                //Check to see if s follows after a ), a number, or a variable, and check to see if s is ) or operator to be syntactically correct.
                if (extraFollowing && (s == ")" || isValid(normalize(s)) || double.TryParse(s, out i)) && s != "+" && s != "-" && s != "*" && s != "/" && s != ")")
                    throw new FormulaFormatException("Any token that immediately follows a number, a variable, or a closing parenthesis must be either an operator or a closing parenthesis.");
                else
                    //If the formula is still syntactically correct, turn extraFollowing false.
                    extraFollowing = false;

                if (s == "(")
                {
                    parBalance++;
                    //The next token will follow the ( which the token will need to be checked.
                    followingOpenPar = true;
                }

                if (s == ")")
                    parBalance--;

                if ((s == ")" || isValid(normalize(s)) || double.TryParse(s, out i)) && s != "+" && s != "-" && s != "*" && s != "/" && s != "(")
                    extraFollowing = true;

                //If s is a valid normalized variable, add the normalized var to tokenList and variables, else just directly add s to tokenList.
                if (isValid(normalize(s)) && s != "+" && s != "-" && s != "*" && s != "/" && s != ")"  && s != "(" && !double.TryParse(s, out i))
                {
                    //Make sure there are no duplicates in variables.
                    if (!variables.Contains(normalize(s)))
                        variables.Add(normalize(s));
                    tokenList.Add(normalize(s));
                }
                else
                {
                    tokenList.Add(s);
                }
            }

            //Check to see if there is at least one token.
            if (tokenCount < 1)
                throw new FormulaFormatException("There must be at least one token.");

            //Check if the first and last tokens makes the formula syntactically correct.
            if (tokens.First() == ")" || tokens.First() == "+" || tokens.First() == "-" || tokens.First() == "*" || tokens.First() == "/")
                throw new FormulaFormatException("The first token of an expression must be a number, a variable, or an opening parenthesis.");
            if (tokens.Last() == "+" || tokens.Last() == "-" || tokens.Last() == "*" || tokens.Last() == "/" || tokens.Last() == "(")
                throw new FormulaFormatException("The last token of an expression must be a number, a variable, or a closing parenthesis.");

            //If the parBalance count is 0, that means ( and ) will balance, if not 0 then they do not, syntactically incorrect.
            if (parBalance != 0)
                throw new FormulaFormatException("Opening and Closing Parenthesis must be equal.");
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            //Use two stacks to hold the tokens, one for operators and one for values.
            Stack<double> values = new Stack<double>();
            Stack<String> operators = new Stack<String>();

            //Go through each substring/token...
            foreach (String s in tokenList)
            {

                //Check for s being an int and if so change it to numericValue.
                if (double.TryParse(s, out double numericValue))
                {
                    //check the operators first item for * or /
                    if (operators.IsOnTopStack("*") || operators.IsOnTopStack("/"))
                    {
                        //If a divide by 0 occurs, the DoTheMath helper method will throw a FormulaFormatException and 
                        //The exception will be caught, to instead return a FormulaError object.
                        try
                        {
                            values.Push(DoTheMath(values, operators.Pop(), numericValue));
                        }
                        catch
                        {
                            return new FormulaError("Error: Can't Divide By Zero.");
                        }
                    }
                    else
                    {
                        //If not / or *, then just push s onto values
                        values.Push(numericValue);
                    }
                    continue;
                }

                //Now check s for adding or subtracting
                if (s is "+" || s is "-")
                {

                    if (operators.IsOnTopStack("+") || operators.IsOnTopStack("-"))
                    {
                        //Add the first two ints in values
                        values.Push(DoTheMath(values, operators.Pop(), values.Pop()));
                    }

                    //Push the operator onto the stack  
                    operators.Push(s);

                    continue;
                }

                //Just push s onto stack if its / or * or a (
                if (s is "/" || s is "*" || s is "(")
                {
                    operators.Push(s);
                    continue;
                }


                if (s is ")")
                {
                    if (operators.IsOnTopStack("+") || operators.IsOnTopStack("-"))
                    {
                        values.Push(DoTheMath(values, operators.Pop(), values.Pop()));
                    }

                    //Then the next operator should be a ( so we will pop it away
                    operators.Pop();

                    //We will be outside the (, so now see if we need to * or / outside
                    if (operators.IsOnTopStack("*") || operators.IsOnTopStack("/"))
                    {
                        try
                        {
                            values.Push(DoTheMath(values, operators.Pop(), values.Pop()));
                        }
                        catch
                        {
                            return new FormulaError("Error: Can't Divide By Zero.");
                        }
                    }

                    continue;
                }

                //Finally, check for variables with the isValid delegate, 
                //the tokenList will already have valid normalized variables from the ctor.
                if (isValid(s))
                {
                    double varValue;
                    //Try using the lookup method on s, because the variable might be undefined.
                    try
                    {
                        varValue = lookup(s);
                    }
                    catch (ArgumentException)
                    {
                        return new FormulaError("Error: Undefined Variables.");
                    }


                    //check the operators first item for * or /
                    if (operators.IsOnTopStack("*") || operators.IsOnTopStack("/"))
                    {
                        try
                        {
                            values.Push(DoTheMath(values, operators.Pop(), varValue));
                        }
                        catch
                        {
                            return new FormulaError("Error: Can't Divide By Zero.");
                        }

                    }
                    else
                    {
                        //If not / or *, then just push s onto values
                        values.Push(varValue);
                    }
                    continue;
                }

            }

            //All tokens have been gone through, so check operators
            if (operators.Count > 0)
            {
                //The operator stack isn't empty, but should contain exactly one + or -
                if (operators.IsOnTopStack("+") || operators.IsOnTopStack("-"))
                {
                    //Add the first two ints in values
                    values.Push(DoTheMath(values, operators.Pop(), values.Pop()));
                }
            }
            //If operators is empty, just return the solution
            return values.Pop();
        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            //The variables list should already contain all the normalized variables without duplicates.
            return variables;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            StringBuilder str = new();
            //Use the string builder to build strings, and
            //tokenList should already have normalized vars too.
            foreach (string s in tokenList)
            {
                //To try to get the same string with 2 or 2.00, using the ToString method on a double will work
                if (double.TryParse(s, out double d))
                    str.Append(d.ToString());
                else
                    str.Append(s);
            }

            //The string builder should now be complete, and have a common string.
            return str.ToString();
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens and variable tokens.
        /// Numeric tokens are considered equal if they are equal after being "normalized" 
        /// by C#'s standard conversion from string to double, then back to string. This 
        /// eliminates any inconsistencies due to limited floating point precision.
        /// Variable tokens are considered equal if their normalized forms are equal, as 
        /// defined by the provided normalizer.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not Formula) return false;

            Formula formula = (Formula)obj;

            //Store all the given obj's tokens into a list
            List<string> comparedFormula = formula.tokenList;

            //Return a bool from the helper method that checks whether the tokens are equal or not
            return checkTokensForEquality(tokenList, comparedFormula, normalize, isValid);
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that f1 and f2 cannot be null, because their types are non-nullable
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {

            return checkTokensForEquality(f1.tokenList, f2.tokenList, s => s, s => true);
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that f1 and f2 cannot be null, because their types are non-nullable
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            //Return the ! of checkTokensForEquality to get the opposite, and it will return if things are not equal.
            return !checkTokensForEquality(f1.tokenList, f2.tokenList, s => s, s => true);
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";
            String rpPattern = @"\)";
            String opPattern = @"[\+\-*/]";
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }

        }

        /// <summary>
        /// Helper class to calculate the result
        /// </summary>
        /// <param name="token"> This is the operator</param>
        /// <param name="leftVal">The left hand side of the expression 
        /// currently getting calculated</param>
        /// <param name="rightVal">The right hand side of current expression</param>
        /// <returns>The result of leftHand (operator) rightHand</returns>
        private static double DoTheMath(Stack<double> stack, string token, double rightVal)
        {
            double result = 0;

            double leftVal = stack.Pop();

            //Use switch case to check operator and do the math.
            switch (token)
            {
                case "+":
                    {
                        result = leftVal + rightVal;
                    }
                    break;
                case "-":
                    {
                        result = leftVal - rightVal;
                    }
                    break;
                case "*":
                    {
                        result = leftVal * rightVal;
                    }
                    break;
                case "/":
                    {
                        if (rightVal == 0)
                            throw new FormulaFormatException("Error: Can't Divide By Zero.");

                        result = leftVal / rightVal;
                    }
                    break;
            }

            return result;
        }

        private static bool checkTokensForEquality(List<string> l1, List<string> l2, Func<string, string> normalize, Func<string, bool> isValid)
        {
            //Check first to see if both formula's have the same amount of tokens.
            if (l1.Count != l2.Count)
                return false;

            //Now check each token for equality.
            for (int i = 0; i < l1.Count; i++)
            {
                if (l1[i] == l2[i])
                    continue;
                //Check each formula's tokens to see if they are numbers, and if they are, check if the number's .ToString()s equal.
                if (Double.TryParse(l1[i], out double tl) && Double.TryParse(l2[i], out double cf) && tl.ToString() == cf.ToString())
                    continue;

                return false;
            }
            //If all tokens have been checked, and are equal, the two formulas are equal.
            return true;
        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }



    }




    /// <summary>
    /// Extension of Stack class for the sake of adding
    /// the IsOnTopStack method.
    /// </summary>
    static class StackExtension
    {
        /// <summary>
        /// This extension method will check if the stack contains anything
        /// and if so will peek and if the top of the stack is the token we need, will
        /// return true, false if otherwise.  Saves from repeated code.
        /// </summary>
        /// <param name="stack"> the stack class which is getting extended</param>
        /// <param name="token">The string we are looking at.</param>
        /// <returns>True if stack contains the needed token, false if not.</returns>
        public static bool IsOnTopStack(this Stack<String> stack, String token)
        {
            return stack.Count > 0 && stack.Peek() == token;
        }
    }

}



