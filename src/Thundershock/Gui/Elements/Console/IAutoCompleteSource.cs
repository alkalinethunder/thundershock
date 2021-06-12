using System.Collections.Generic;

namespace Thundershock.Gui.Elements.Console
{
    public interface IAutoCompleteSource
    {
        bool IsWhiteSpace(char ch)
        {
            return char.IsWhiteSpace(ch);
        }
        
        IEnumerable<string> GetCompletions(string word);
    }
}