
/*
 * phosphorus five, copyright 2014 - Mother Earth, Jannah, Gaia
 * phosphorus five is licensed as mit, see the enclosed LICENSE file for details
 */

using System.Collections.Generic;
using phosphorus.core;
using phosphorus.expressions;
using phosphorus.expressions.exceptions;

namespace phosphorus.lambda
{
    /// <summary>
    /// class wrapping [override] keyword
    /// </summary>
    public static class pfOverride
    {
        /// <summary>
        /// overrides zero or more events, with all given [super] parameters
        /// </summary>
        /// <param name="context"><see cref="phosphorus.Core.ApplicationContext"/> for Active Event</param>
        /// <param name="e">parameters passed into Active Event</param>
        [ActiveEvent (Name = "override")]
        private static void lambda_override (ApplicationContext context, ActiveEventArgs e)
        {
            // syntax checking
            var overrideNodes = new List<Node> (e.Args.FindAll (
            delegate (Node idx) {
                return idx.Name == "super";
            }));
            if (overrideNodes.Count != 1)
                throw new LambdaException ("[override] requires exactly one [super] parameter", e.Args, context);

            // retrieving all overrides
            var overrides = new List<string> (XUtil.Iterate<string> (overrideNodes [0], context));

            // iterating through each override event, creating our override
            foreach (var idxBase in XUtil.Iterate<string> (e.Args, context)) {
                foreach (var idxSuper in overrides) {
                    events_common.CreateOverride (context, idxBase, idxSuper);
                }
            }
        }
    }
}
