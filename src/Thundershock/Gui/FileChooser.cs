using System;
using System.Collections.Generic;
using System.Threading;
using Gtk;
using System.Text;
using MimeTypes.Core;

#if WINDOWS
using System.Windows.Forms;
#endif

namespace Thundershock.Gui
{
    public class FileChooser
    {
        private string _title = "Open File";
        private List<string> _acceptedTypes = new();
        private AppBase _app;
        private string _path;

        public string SelectedFilePath => _path;
        
        public FileOpenerType FileOpenerType { get; set; }

        public bool AllowAnyFileType { get; set; } = true;
        
        public string InitialDirectory { get; set; }

        public ICollection<string> AcceptedFileTypes => _acceptedTypes;
        
        public string Title
        {
            get => _title;
            set => _title = value ?? "Open File";
        }

        public FileChooser(AppBase app)
        {
            _app = app;

            InitialDirectory = Environment.CurrentDirectory;
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

                var extension = MimeTypeMap.GetExtension(acceptedType);

                sb.Append(extension.ToUpper());
                sb.Append(" Files|*");
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

        private ManualResetEvent _gtkReset = new ManualResetEvent(false);
        
        private ResponseType GtkShowDialog()
        {
            Gtk.Application.Init();

            var result = Gtk.ResponseType.Ok;
            var chooser = new Gtk.FileChooserDialog(Title, null, GetGtkAction(), GetGtkButtonData());

            chooser.SetCurrentFolder(InitialDirectory);

            foreach (var mime in _acceptedTypes)
            {
                var filter = new FileFilter();
                filter.Name = mime;
                filter.AddMimeType(mime);
                chooser.AddFilter(filter);
            }
            
            chooser.Response += (o, args) =>
            {
                _path = chooser.Filename;
                result = args.ResponseId;

                chooser.Hide();
                
                Gtk.Application.Quit();
            };
            
            chooser.Show();

            Gtk.Application.Run();

            return result;
        }

        private FileChooserAction GetGtkAction()
        {
            return FileOpenerType switch
            {
                FileOpenerType.Open => FileChooserAction.Open,
                FileOpenerType.Save => FileChooserAction.Save,
                Gui.FileOpenerType.FolderTree => FileChooserAction.SelectFolder
            };
        }

        private object[] GetGtkButtonData()
        {
            return FileOpenerType switch
            {
                Gui.FileOpenerType.Open => new object[]
                    {"Cancel", Gtk.ResponseType.Cancel, "Open", Gtk.ResponseType.Ok},
                Gui.FileOpenerType.Save => new object[]
                    {"Cancel", Gtk.ResponseType.Cancel, "Save", Gtk.ResponseType.Ok},
                Gui.FileOpenerType.FolderTree => new object[]
                    {"Cancel", Gtk.ResponseType.Cancel, "Choose Folder", Gtk.ResponseType.Ok},
            };
        }
        
        public FileOpenerResult Activate()
        {
#if WINDOWS
            var result = ShowWindowsDialog();
            
            return MapDialogResult(result);
#else
            var result = GtkShowDialog();

            return result switch
            {
                ResponseType.Ok => FileOpenerResult.OK,
                _ => FileOpenerResult.Cancelled
            };
#endif
        }
    }
}