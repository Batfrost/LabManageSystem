// Skeleton implementation written by Joe Zachary for CS 3500, September 2013.
// Version 1.1 (Fixed error in comment for RemoveDependency.)
// Version 1.2 - Daniel Kopta 
//               (Clarified meaning of dependent and dependee.)
//               (Clarified names in solution/project structure.)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// (s1,t1) is an ordered pair of strings
    /// t1 depends on s1; s1 must be evaluated before t1
    /// 
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        (The set of things that depend on s)    
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    ///        (The set of things that s depends on) 
    //
    // For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    //     dependents("a") = {"b", "c"}
    //     dependents("b") = {"d"}
    //     dependents("c") = {}
    //     dependents("d") = {"d"}
    //     dependees("a") = {}
    //     dependees("b") = {"a"}
    //     dependees("c") = {"a"}
    //     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        
        private IDictionary<string, HashSet<string>> dependents;
        private IDictionary<string, HashSet<string>> dependees;
        private int size;

        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph()
        {
            dependents = new Dictionary<string, HashSet<string>>();
            dependees = new Dictionary<string, HashSet<string>>();
            size = 0;
        }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            get { return size; }
        }


        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        public int this[string s]
        {
            get 
            { 
                if (dependees.ContainsKey(s))
                    return dependees[s].Count;
                return 0;
            }
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s)
        {   

            if (dependents.ContainsKey(s) && dependents[s].Count > 0)
                return true;
            return false;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s)
        {
            if (dependees.ContainsKey(s) && dependees[s].Count > 0)
                return true;
            return false;
        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            HashSet<string> pendents = new HashSet<string>();

            if (dependents.ContainsKey(s))
                pendents = dependents[s];

            
            return pendents;
        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            HashSet<string> pendees = new HashSet<string>();
            if (dependees.ContainsKey(s))
                pendees = dependees[s];

            return pendees;
        }


        /// <summary>
        /// <para>Adds the ordered pair (s,t), if it doesn't exist</para>
        /// 
        /// <para>This should be thought of as:</para>   
        /// 
        ///   t depends on s
        ///
        /// </summary>
        /// <param name="s"> s must be evaluated first. T depends on S</param>
        /// <param name="t"> t cannot be evaluated until s is</param>        /// 
        public void AddDependency(string s, string t)
        {
            //Call helper method and check to see if it returns true
            if (CheckAndAdd(s, t, dependents))
            {
                //If true, something was added, continue with adding for dependees this time
                CheckAndAdd(t, s, dependees);
                //Increase the size, because something was added
                size++;
            }
             //If CheckAndAdd returns false, nothing new was added so do nothing
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {
            //First check if (s, t) exists, and if so remove (s, t) from dependents, and (t, s) from dependees.
            if (dependents.ContainsKey(s) && dependents.TryGetValue(s, out HashSet<string>? p) && p.Contains(t))
            {
                dependents[s].Remove(t);
                dependees[t].Remove(s);
                size--;
            }
                
            //If the pair of (s, t) does not exist, do nothing.

        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            //Remove s from list of dependents
            
            if (dependents.ContainsKey(s))
            {
                size -= dependents[s].Count;
                foreach (string t in dependents[s])
                {
                    RemoveDependency(s, t);
                    size++;
                }
            }
                
            //Use AddDependency on s,t for dependents
            foreach (string t in newDependents)
                if (t != "")
                    AddDependency(s, t);
            
                
            
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {

            //Remove s from list of dependees
            if (dependees.ContainsKey(s))
            {
                size -= dependees[s].Count;
                foreach (string t in dependees[s])
                { 
                    RemoveDependency(t, s);
                    size++;
                }
            }

            //Use AddDependency on t,s for dependees
            foreach (string t in newDependees)
            {
                if (t != "")
                    AddDependency(t, s);
            }
                
        }

        /// <summary>
        /// Helper method for checking if a given Dictionary contains 
        /// the given key s, and if not will add the (s, t) pair.
        /// But if the dictionary does have a key s, then the method 
        /// will check if there is a value t to the key, if not add it.
        /// </summary>
        /// <param name="s">Method will check if this given Key is in it</param>
        /// <param name="t">Method will check if this given Value matches the given Key</param>
        /// <param name="dicto">The Dictionary object getting checked</param>
        /// <returns>Will Return true if something was added, and false if nothing was added</returns>
        private bool CheckAndAdd(string s, string t, IDictionary<string, HashSet<string>> dicto) 
        {
            //First check if the Dictionary has the key
            if (!dicto.ContainsKey(s))
            {
                //If no key, add the key and it's value t.
                dicto.Add(s, new HashSet<string>());
                dicto[s].Add(t);
                //Something was added, return true
                return true;
            }
            else if (!(dicto.TryGetValue(s, out HashSet<string>? p) && p.Contains(t))) 
            {
                //If the key s exists, but there is no (s, t) add t to s.
                dicto[s].Add(t);
                //Something was added, return true
                return true;
            } 
            else
            {
                //Nothing was added, so return false
                return false;
            }
            
        }

    }

}
