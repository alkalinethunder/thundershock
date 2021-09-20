using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace Thundershock.Core.Utilities
{
    /// <summary>
    /// Provides some incredibly useful methods and algorithms for transforming and manipulating strings of text.
    /// </summary>
    public static class TextHelpers
    {
        /// <summary>
        /// Transforms a given string of  text into a C#-friendly identifier string.
        /// </summary>
        /// <param name="textString">The string of text to transform.</param>
        /// <returns>A string representing a friendly C# identifier made from the given source text.</returns>
        public static string MakeIdentifier(string textString)
        {
            // Trim the text to get rid of trailing and leading spaces.
            var trimmed = textString.Trim();

            // Split it into words.
            var words = GetTextWords(textString);
            
            // Go through each word, and strip unwanted characters. Also capitalize it.
            for (var i = 0; i < words.Length; i++)
            {
                // Strips non-alphanumeric characters.
                var word = StripCharacters(words[i],
                    x => (char.IsLetterOrDigit(x) || x == '_') && x != '.' && x != ',');

                // If this is the first word and the first character is a digit, then we have to make sure we
                // do something about that.
                //
                // Many approaches will work, but I'll go with replacing the digit with its English name. (3 => "Three")
                if (i == 0 && char.IsDigit(word[0]))
                {
                    var digit = word[0];
                    var digitName = digit switch
                    {
                        '0' => "Zero",
                        '1' => "One",
                        '2' => "Two",
                        '3' => "Three",
                        '4' => "Four",
                        '5' => "Five",
                        '6' => "Six",
                        '7' => "Seven",
                        '8' => "Eight",
                        '9' => "Nine"
                    };

                    word = digitName + word.Substring(1);
                }
                
                // Capitalize the word.
                word = char.ToUpperInvariant(word[0]).ToString() + word.Substring(1);

                words[i] = word;
            }
            
            // Result  is all words joined together.
            return string.Join("", words);
        }

        public static string[] GetTextWords(string textString)
        {
            var word = string.Empty;
            var words = new List<string>();

            for (var i = 0; i <= textString.Length; i++)
            {
                if (i == textString.Length)
                {
                    if (!string.IsNullOrWhiteSpace(word))
                        words.Add(word);
                }
                else
                {
                    var character = textString[i];
                    if (char.IsWhiteSpace(character))
                    {
                        if (!string.IsNullOrWhiteSpace(word))
                            words.Add(word);
                        word = string.Empty;
                    }
                    else
                    {
                        word += character;
                    }
                }
            }
            
            return words.ToArray();
        }

        public static string StripCharacters(string text, Expression<Func<char, bool>> condition)
        {
            var func = condition.Compile();

            var result = new StringBuilder();

            foreach (var character in text)
            {
                if (func(character))
                    result.Append(character.ToString());
            }
            
            return result.ToString();
        }

        public static string StripWhiteSpace(string textString)
            => StripCharacters(textString, x => !char.IsWhiteSpace(x));
    }
}