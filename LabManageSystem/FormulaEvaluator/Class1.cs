using System.Text.RegularExpressions;

namespace FormulaEvaluator
{
    /// <summary>
    /// This class will take in an in-Fix expression such as 
    /// (5 + 1) and return the solution. Uses a delegate to 
    /// look up values of variables that can be used in the
    /// expressions.
    /// </summary>
    public static class Evaluator
    {
        //Create the delegate for the Lookup method to be used later
        public delegate int Lookup(String v);

        /// <summary>
        /// This method evaluate the given expression and return the result
        /// </summary>
        /// <param name="exp">This is the full expression in string form,
        /// but will be broken down into tokens</param>
        /// <param name="variableEvaluator">The delegate method that will 
        /// be called and used when the expression contains variables.</param>
        /// <returns>Will return the result of the expression as an int.</returns>
        public static int Evaluate(String exp, Lookup variableEvaluator)
        {
            //Get rid of extra white space
            exp = exp.Replace(" ", String.Empty);
            
            //Break down exp (after trimming the white space) into tokens: operators, numbers and variables.
            string[] substrings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");

            //Use two stacks to hold the tokens, one for operators and one for values.
            Stack<int> values = new Stack<int>();
            Stack<String> operators = new Stack<String>();

            //Go through each substring/token...
            foreach (String s in substrings)
            {
                //If s is empty string, just continue
                if (s.Length == 0)
                {
                    continue;
                }

                //Check for s being an int and if so change it to numericValue.
                if (int.TryParse(s, out int numericValue))
                {
                    //check the operators first item for * or /
                    if (operators.IsOnTopStack("*") || operators.IsOnTopStack("/"))
                    {
                        values.Push(DoTheMath(values, operators.Pop(), numericValue));
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
                        values.Push(DoTheMath(values, operators.Pop(), values.Pop()));
                    }

                    continue;
                }

                //Finally, check for variables
                if (Regex.IsMatch(s, "^[a-zA-Z]+[0-9]+$"))
                {
                    int varValue = variableEvaluator(s);
                    //check the operators first item for * or /
                    if (operators.IsOnTopStack("*") || operators.IsOnTopStack("/"))
                    {
                        values.Push(DoTheMath(values, operators.Pop(), varValue));
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
        /// Helper class to calculate the result
        /// </summary>
        /// <param name="token"> This is the operator</param>
        /// <param name="leftVal">The left hand side of the expression 
        /// currently getting calculated</param>
        /// <param name="rightVal">The right hand side of current expression</param>
        /// <returns>The result of leftHand (operator) rightHand</returns>
        private static int DoTheMath(Stack<int> stack, string token, int rightVal)
        {
            int result = 0;
            
            int leftVal = stack.Pop();

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
                            throw new ArgumentException("Error: Dividing by zero.");
                        
                        result = leftVal / rightVal;
                    }
                    break;
            }

            return result;
        }

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
