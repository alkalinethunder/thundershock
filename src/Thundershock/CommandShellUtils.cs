using System.Collections.Generic;
using System.Data;

namespace Thundershock
{
    public static class CommandShellUtils
    {
        public static string[] BreakLine(string commandLine)
        {
            var inQuote = false;
            var inEscape = false;
            var quote = '"';
            var escape = '\\';
            var words = new List<string>();
            var word = "";

            for (var i = 0; i <= commandLine.Length; i++)
            {
                if (i == commandLine.Length)
                {
                    if (inQuote)
                        throw new SyntaxErrorException("unterminated string");

                    if (inEscape)
                        throw new SyntaxErrorException("unexpected escape sequence");

                    if (!string.IsNullOrEmpty(word))
                    {
                        words.Add(word);
                        word = string.Empty;
                    }
                }
                else
                {
                    var ch = commandLine[i];

                    if (inEscape)
                    {
                        word += ch;
                        inEscape = false;
                        continue;
                    }

                    if (ch == escape)
                    {
                        inEscape = true;
                        continue;
                    }

                    if (inQuote)
                    {
                        if (ch == quote)
                        {
                            inQuote = false;
                            continue;
                        }

                        word += ch;
                    }
                    else
                    {
                        if (ch == quote)
                        {
                            inQuote = true;
                            continue;
                        }

                        if (char.IsWhiteSpace(ch))
                        {
                            if (!string.IsNullOrEmpty(word))
                            {
                                words.Add(word);
                                word = string.Empty;
                            }

                            continue;
                        }

                        word += ch;
                    }
                }
            }
            
            return words.ToArray();
        }

    }
}