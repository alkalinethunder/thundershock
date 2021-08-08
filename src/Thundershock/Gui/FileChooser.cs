using System;
using System.Collections.Generic;
using Gtk;

namespace Thundershock.Gui
{
    public partial class FileChooser
    {
        private string _title = "Open File";
        private List<AcceptedFileType> _acceptedTypes = new();
        private string _path;

        public string SelectedFilePath => _path;

        public FileOpenerType FileOpenerType { get; set; }

        public bool AllowAnyFileType { get; set; } = true;

        public string InitialDirectory { get; set; }

        public ICollection<AcceptedFileType> AcceptedFileTypes => _acceptedTypes;

        public string Title
        {
            get => _title;
            set => _title = value ?? "Open File";
        }

        public FileChooser()
        {
            InitialDirectory = Environment.CurrentDirectory;
        }

        public void AcceptFileType(string extension, string name)
        {
            var ftype = new AcceptedFileType
            {
                Name = name,
                Extension = extension
            };

            _acceptedTypes.Add(ftype);
        }

#if WINDOWS
        private System.Windows.Forms.DialogResult ShowWindowsDialog()
        {
            var dialogResult = System.Windows.Forms.DialogResult.OK;

            switch (FileOpenerType)
            {
                case FileOpenerType.Open:
                    var openFileDialog = new OpenFileDialog();

                    openFileDialog.Filter = BuildWindowsFileFilter();
                    openFileDialog.Title = Title;
                    openFileDialog.InitialDirectory = InitialDirectory;

                    dialogResult = openFileDialog.ShowDialog();

                    _path = openFileDialog.FileName;

                    break;
                case FileOpenerType.Save:

                    break;
                case FileOpenerType.FolderTree:

                    break;
            }

            return dialogResult;
        }

        private string BuildWindowsFileFilter()
        {
            var sb = new StringBuilder();

            foreach (var acceptedType in _acceptedTypes)
            {
                if (sb.Length > 0)
                    sb.Append("|");

                var extension = acceptedType.Extension;

                sb.Append(acceptedType.Name);
                sb.Append("|*.");
                sb.Append(extension);
            }

            if (AllowAnyFileType)
            {
                if (sb.Length > 0)
                    sb.Append("|");

                sb.Append("All files|*.*"); // OwO
            }

            return sb.ToString();
        }

        private FileOpenerResult MapDialogResult(System.Windows.Forms.DialogResult result)
        {
            return result switch
            {
                System.Windows.Forms.DialogResult.OK => FileOpenerResult.OK,
                _ => FileOpenerResult.Cancelled
            };
        }
#endif

        private ResponseType GtkShowDialog()
        {
            Application.Init();

            var result = ResponseType.Ok;
            var chooser = new FileChooserDialog(Title, null, GetGtkAction(), GetGtkButtonData());

            chooser.SetCurrentFolder(InitialDirectory);

            foreach (var ftype in _acceptedTypes)
            {
                var filter = new FileFilter();
                filter.Name = ftype.Name;
                filter.AddPattern($"*.{ftype.Extension}");
                chooser.AddFilter(filter);
            }

            chooser.Response += (_, args) =>
            {
                _path = chooser.Filename;
                result = args.ResponseId;

                chooser.Hide();

                Application.Quit();
            };

            chooser.Show();

            Application.Run();

            return result;
        }

        private FileChooserAction GetGtkAction()
        {
            return FileOpenerType switch
            {
                FileOpenerType.Open => FileChooserAction.Open,
                FileOpenerType.Save => FileChooserAction.Save,
                FileOpenerType.FolderTree => FileChooserAction.SelectFolder,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private object[] GetGtkButtonData()
        {
            return FileOpenerType switch
            {
                FileOpenerType.Open => new object[]
                    {"Cancel", ResponseType.Cancel, "Open", ResponseType.Ok},
                FileOpenerType.Save => new object[]
                    {"Cancel", ResponseType.Cancel, "Save", ResponseType.Ok},
                FileOpenerType.FolderTree => new object[]
                    {"Cancel", ResponseType.Cancel, "Choose Folder", ResponseType.Ok},
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public FileOpenerResult Activate()
        {
            if (ThundershockPlatform.IsPlatform(Platform.Windows))
                return Win32AskForFile();
            else
            {
                var result = GtkShowDialog();

                return result switch
                {
                    ResponseType.Ok => FileOpenerResult.Ok,
                    _ => FileOpenerResult.Cancelled
                };
            }
        }
    }
    
    public class AcceptedFileType
    { 
        public string Extension { get; set; }
        public string Name { get; set; }
    }
}
