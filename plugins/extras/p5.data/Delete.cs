/*
 * Phosphorus Five, copyright 2014 - 2016, Thomas Hansen, phosphorusfive@gmail.com
 * Phosphorus Five is licensed under the terms of the MIT license, see the enclosed LICENSE file for details
 */

using System.Collections.Generic;
using p5.exp;
using p5.core;
using p5.data.helpers;
using p5.exp.exceptions;

namespace p5.data
{
    /// <summary>
    ///     Class wrapping [delete-data]
    /// </summary>
    public static class Delete
    {
        /// <summary>
        ///     Deletes items from your database
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Parameters passed into Active Event</param>
        [ActiveEvent (Name = "delete-data", Protection = EventProtection.LambdaClosed)]
        public static void delete_data (ApplicationContext context, ActiveEventArgs e)
        {
            // Basic syntax checking
            if (!XUtil.IsExpression (e.Args.Value))
                throw new LambdaException ("[delete-data] requires an expression as its value", e.Args, context);

            // Making sure we clean up and remove all arguments passed in after execution
            using (new Utilities.ArgsRemover (e.Args)) {

                // Acquiring lock on database
                lock (Common.Lock) {

                    // Used to store how many items are actually affected
                    int affectedItems = 0;

                    // Looping through database matches and removing nodes while storing which files have been changed
                    var changed = new List<Node> ();
                    foreach (var idxDest in e.Args.Get<Expression> (context).Evaluate (context, Common.Database, e.Args)) {

                        // Figuring out which file Node updated belongs to, and storing in changed list
                        Common.AddNodeToChanges (idxDest.Node, changed);

                        // Setting value to null, which works if user chooses to delete "value", "name" and/or "node"
                        // Path and count though will throw an exception
                        idxDest.Value = null;

                        // Incrementing affected items
                        affectedItems += 1;
                    }

                    // Saving all affected files
                    Common.SaveAffectedFiles (context, changed);

                    // Returning number of affected items
                    e.Args.Value = affectedItems;
                }
            }
        }
    }
}