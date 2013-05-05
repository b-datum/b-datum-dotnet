using System;
using System.Collections.Generic;
using System.Text;

using System.Text.RegularExpressions;

namespace bdatum
{

    // Curl class to process the curl commands, with some hardcoded commands

    class CurlCommand
    {


    }



    class bHttpResponse
    {
        public Dictionary<string, string> Error = new Dictionary<string, string>();

        public bHttpResponse(string output, string error)
        {
            string[] lines = error.Split('\r', '\n');

            Regex headers_rx = new Regex(@"<\s(?<token>[\w|\-|\s]+):(?<value>.*)");
            //   HTTP/1.1 404 Not Found
            Regex httpstatus_rx = new Regex(@"<\sHTTP\/1\.1\s(?<code>\d+)\s(?<message>\w+)");

            foreach (string line in lines)
            {
                MatchCollection matches = headers_rx.Matches(line);
                MatchCollection http_matches = httpstatus_rx.Matches(line);

                if (http_matches.Count > 0)
                {
                    foreach (Match match in http_matches)
                    {
                        string code = match.Groups["code"].Value;
                        if (Error.ContainsKey("HTTP"))
                        {
                            Error["HTTP"] = code;
                        }
                        else
                        {
                            Error.Add("HTTP", code);
                        }
                    }
                }

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        string token = match.Groups["token"].Value;
                        string value = match.Groups["value"].Value;
                        var stop = "stop";
                        // check if it is rewrite 
                        if (Error.ContainsKey(token))
                        {
                            Error[token] = value;
                        }
                        else
                        {
                            Error.Add(token, value);
                        }
                    }
                }
            }

        }

        public string statuscode()
        {
            if (Error.ContainsKey("HTTP"))
            {
                return Error["HTTP"];
            }else{
                return "No HTTP connection";
            }
        }

        public bool status()
        {
            if (Error.ContainsKey("HTTP") && Error["HTTP"].StartsWith("2"))
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
