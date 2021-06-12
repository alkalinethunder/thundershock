namespace Thundershock.Gui.Elements.Console
{
    public interface IConsole
    {
        IAutoCompleteSource AutoCompleteSource { get; set; }
        
        void Write(object value);
        void Write(string valuue);
        void WriteLine(object value);
        void WriteLine(string value);
        void Write(string format, params object[] values);
        void WriteLine(string format, params object[] values);
        void WriteLine();
        void Clear();

        bool GetLine(out string text);
        bool GetCharacter(out char character);
    }
}