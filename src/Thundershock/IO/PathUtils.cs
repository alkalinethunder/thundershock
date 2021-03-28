using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Thundershock.IO
{
    public static class PathUtils
    {
        public static readonly string Separator = "/";
        public static readonly string CurrentDirectory = ".";
        public static readonly string ParentDirectory = "..";
        public static readonly string Home = "~";

        public static string GetFileName(string path)
        {
            return Split(path).LastOrDefault();
        }

        public static string GetDirectoryName(string path)
        {
            var parts = Split(path);
            return Combine(parts.Take(parts.Length - 1).ToArray());
        }

        public static string Combine(params string[] parts)
        {
            var path = Separator;

            foreach (var part in parts)
            {
                var j = part;
                while (j.StartsWith(Separator))
                {
                    j = j.Substring(Separator.Length);
                }

                if (!path.EndsWith(Separator))
                    path += Separator;

                path += j;
            }
            
            return path;
        }

        public static string[] Split(string path)
        {
            return path.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string Resolve(string path)
        {
            var split = Split(path);
            return Resolve(split);
        }

        public static string Resolve(params string[] path)
        {
            var stack = new Stack<string>();
            var parts = new List<string>();
            
            // pass 1, take care of dumbass users
            foreach (var part in path)
            {
                if (part.Contains(Separator))
                {
                    var j = part;
                    do
                    {
                        var substr = j.Substring(0, j.IndexOf(Separator));
                        if (!string.IsNullOrEmpty(substr))
                        {
                            parts.Add(substr);
                        }

                        j = j.Substring(substr.Length + Separator.Length);

                    } while (j.Contains(Separator));

                    if (!string.IsNullOrEmpty(j))
                    {
                        parts.Add(j);
                    }
                }
                else
                {
                    parts.Add(part);
                }
            }
            
            // pass 2, actually perform the resolve
            foreach (var part in parts)
            {
                if (part == CurrentDirectory)
                    continue;

                if (part == ParentDirectory)
                {
                    if (stack.Count > 0)
                        stack.Pop();
                    continue;
                }

                stack.Push(part);
            }

            return Combine(stack.ToArray().Reverse().ToArray());
        }
    }
}