using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace

namespace ArachNGIN.CommandLine
{
    /// <summary>
    ///     Parses command line arguments.
    /// </summary>
    public class Parameters
    {
        /// <summary>
        ///     Collection of parameters in key value format
        /// </summary>
        private readonly StringDictionary _dict = new StringDictionary();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Parameters" /> class and parses command line arguments
        /// </summary>
        /// <param name="args">The arguments.</param>
        public Parameters(IEnumerable<string> args)
        {
            var splitter = new Regex(@"^-{1,2}|^/|=|:(?!\\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var isParam = new Regex(@"^^-{1,2}|^/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            string currentParameter = null;
            foreach (var s in args)
            {
                string[] parts;
                if (isParam.IsMatch(s)) parts = splitter.Split(s, 3);
                else parts = new[] { s };
                //var parts = splitter.Split(s, 3)
                switch (parts.Length)
                {
                    case 1:
                        if (currentParameter != null)
                        {
                            if (!_dict.ContainsKey(currentParameter))
                            {
                                parts[0] = remover.Replace(parts[0], "$1");
                                _dict.Add(currentParameter, parts[0]);
                            }
                            currentParameter = null;
                        }
                        else
                        {
                            // add unmatched parameters as "false" (without -, / or --)
                            if (!_dict.ContainsKey(parts[0])) _dict.Add(parts[0], "false");
                        }
                        break;

                    case 2:
                        if (currentParameter != null && !_dict.ContainsKey(currentParameter))
                            _dict.Add(currentParameter, "true");
                        currentParameter = parts[1];
                        break;

                    case 3:
                        if (currentParameter != null && !_dict.ContainsKey(currentParameter))
                            _dict.Add(currentParameter, "true");
                        currentParameter = parts[1];
                        if (!_dict.ContainsKey(currentParameter))
                        {
                            parts[2] = remover.Replace(parts[2], "$1");
                            _dict.Add(currentParameter, parts[2]);
                        }
                        currentParameter = null;
                        break;
                }
            }
            if (currentParameter != null && !_dict.ContainsKey(currentParameter)) _dict.Add(currentParameter, "true");
        }

        /// <summary>
        ///     Gets the <see cref="System.String" /> with the specified parameter.
        /// </summary>
        /// <value>
        ///     The <see cref="System.String" />.
        /// </value>
        /// <param name="param">The parameter.</param>
        /// <returns></returns>
        public string this[string param]
        {
            get { return _dict[param]; }
        }
    }
}