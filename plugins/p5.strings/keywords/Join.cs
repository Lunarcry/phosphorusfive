/*
 * Phosphorus Five, copyright 2014 - 2016, Thomas Hansen, thomas@gaiasoul.com
 * 
 * This file is part of Phosphorus Five.
 *
 * Phosphorus Five is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3, as published by
 * the Free Software Foundation.
 *
 *
 * Phosphorus Five is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Phosphorus Five.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * If you cannot for some reasons use the GPL license, Phosphorus
 * Five is also commercially available under Quid Pro Quo terms. Check 
 * out our website at http://gaiasoul.com for more details.
 */

using System.Text;
using p5.exp;
using p5.core;

namespace p5.strings.keywords
{
    /// <summary>
    ///     Class wrapping the [p5.string.join] keyword in p5 lambda.
    /// </summary>
    public static class Join
    {
        /// <summary>
        ///     The [p5.string.join] keyword, allows you to p5.string.join multiple nodes/names/values into a single string
        /// </summary>
        /// <param name="context">Application Context</param>
        /// <param name="e">Parameters passed into Active Event</param>
        [ActiveEvent (Name = "p5.string.join")]
        public static void p5_string_join (ApplicationContext context, ActiveEventArgs e)
        {
            // Making sure we clean up and remove all arguments passed in after execution
            using (new Utilities.ArgsRemover (e.Args)) {

                // Retrieving separator character(s)
                var insertBetweenNodes = e.Args.GetExChildValue ("sep", context, "");

                // Retrieving wrapper character(s)
                var wrapValue = e.Args.GetExChildValue ("wrap", context, "");

                // Used as buffer
                StringBuilder result = new StringBuilder ();

                // Looping through each value
                foreach (var idx in XUtil.Iterate<string> (context, e.Args)) {

                    // Checking if this is first instance, and if not, we add separator value
                    if (result.Length != 0)
                        result.Append (insertBetweenNodes);

                    // Adding currently iterated result
                    result.Append (wrapValue + idx + wrapValue);
                }

                // Returning result
                e.Args.Value = result.ToString ();
            }
        }
    }
}
