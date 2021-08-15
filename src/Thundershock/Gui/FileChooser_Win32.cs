using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Thundershock.Gui
{
    // Win32 platform implementation for the Thundershock FileChooser APIs.
    //
    // This contains the platform bindings needed for Windows native choosers, courtesy of
    // Avalonia's source code: https://github.com/AvaloniaUI/Avalonia/.
    //
    // If you suggest using Windows Forms, then you can rewrite this engine in C++ because
    // that's a way fucking better option.
    public partial class FileChooser
    {
        private const FOS DefaultFileOptions = FOS.FOS_FORCEFILESYSTEM | FOS.FOS_NOVALIDATE | FOS.FOS_NOTESTFILECREATE |
                                               FOS.FOS_DONTADDTORECENT;
        #region Platform bindings - Win32
        
        private enum SIATTRIBFLAGS
        {
            SIATTRIBFLAGS_AND = 1,
            SIATTRIBFLAGS_APPCOMPAT = 3,
            SIATTRIBFLAGS_OR = 2
        }

        [Flags]
        private enum FOS : uint
        {
            FOS_OVERWRITEPROMPT = 0x00000002,
            FOS_STRICTFILETYPES = 0x00000004,
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040, // Ensure that items returned are filesystem items.
            FOS_ALLNONSTORAGEITEMS = 0x00000080, // Allow choosing items that have no storage.
            FOS_NOVALIDATE = 0x00000100,
            FOS_ALLOWMULTISELECT = 0x00000200,
            FOS_PATHMUSTEXIST = 0x00000800,
            FOS_FILEMUSTEXIST = 0x00001000,
            FOS_CREATEPROMPT = 0x00002000,
            FOS_SHAREAWARE = 0x00004000,
            FOS_NOREADONLYRETURN = 0x00008000,
            FOS_NOTESTFILECREATE = 0x00010000,
            FOS_HIDEMRUPLACES = 0x00020000,
            FOS_HIDEPINNEDPLACES = 0x00040000,
            FOS_NODEREFERENCELINKS = 0x00100000,
            FOS_DONTADDTORECENT = 0x02000000,
            FOS_FORCESHOWHIDDEN = 0x10000000,
            FOS_DEFAULTNOMINIMODE = 0x20000000
        }
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszSpec;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }
        
        [ComImport(), Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig()]
            uint Show([In, Optional] IntPtr hwndOwner); //IModalWindow


            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypes(uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] COMDLG_FILTERSPEC[] rgFilterSpec);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypeIndex([In] uint iFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileTypeIndex(out uint piFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Unadvise([In] uint dwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOptions([In] uint fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetOptions(out uint fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Close([MarshalAs(UnmanagedType.Error)] uint hr);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetClientGuid([In] ref Guid guid);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint ClearClientData();

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

        }

        [ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig()]
            uint Show([In, Optional] IntPtr hwndOwner); //IModalWindow 

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetFileTypeIndex([In] uint iFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFileTypeIndex(out uint piFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Unadvise([In] uint dwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOptions([In] uint fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetOptions(out uint fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Close([MarshalAs(UnmanagedType.Error)] int hr);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetClientGuid([In] ref Guid guid);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ClearClientData();

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
        }

        [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid rbhid,
                         [In] ref Guid riid, out IntPtr ppvOut);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetPropertyStore([In] int Flags, [In] ref Guid riid, out IntPtr ppv);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetPropertyDescriptionList([In] ref PROPERTYKEY keyType, [In] ref Guid riid, out IntPtr ppv);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetAttributes([In] SIATTRIBFLAGS dwAttribFlags, [In] uint sfgaoMask, out uint psfgaoAttribs);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCount(out uint pdwNumItems);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetItemAt([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
        }

        
        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint BindToHandler([In] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IntPtr ppvOut);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetDisplayName([In] uint sigdnName, out IntPtr ppszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);

        }
        
        private enum HRESULT : uint
        {
            S_FALSE = 0x0001,
            S_OK = 0x0000,
            E_INVALIDARG = 0x80070057,
            E_OUTOFMEMORY = 0x8007000E,
            E_NOTIMPL = 0x80004001,
            E_UNEXPECTED = 0x8000FFFF
        }

        
        private static readonly Guid ClsOpenFileDialog = Guid.Parse("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
        private static readonly  Guid ClsSaveFileDialog = Guid.Parse("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
        private static readonly Guid ClsIFileDialog = Guid.Parse("42F85136-DB7E-439C-85F1-E4075D135FC8");
        private static readonly Guid ClsIShellItem = Guid.Parse("43826D1E-E718-42EE-BC55-A1E261C37BFE");

        #region WinAPI platform bindings

        [DllImport("ole32.dll", PreserveSig = true)]
        private static extern int CoCreateInstance(ref Guid clsid,
            IntPtr ignore1, int ignore2, ref Guid iid, [MarshalAs(UnmanagedType.IUnknown), Out] out object pUnkOuter);
        
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        private const uint SIGDN_FILESYSPATH = 0x80058000;

        #endregion

        
        #endregion
        
        #region Attachment to the engine

        private FileOpenerResult Win32AskForFile()
        {
            // Folder  choosers...
            if (this.FileOpenerType == FileOpenerType.FolderTree)
            {
                return Win32AskForFolder();
            }
            
            // result of the Thundershock native dialog.
            var result = FileOpenerResult.Cancelled;

            // get the class id for either an open dialog or save dialog depending on what the user asked for
            var clsId = this.FileOpenerType == FileOpenerType.Open ? ClsOpenFileDialog : ClsSaveFileDialog;

            // Uhhhhhhhhh Win32 bloat
            var iid = ClsIFileDialog;

            // Create the dialog and get ourselves a fancy dancy object reference to it.
            CoCreateInstance(ref clsId, IntPtr.Zero, 1, ref iid, out var obj);

            // And cast that object reference so we can fuck with it.
            var fd = (IFileDialog) obj;

            // Set default dialog options for the file dialog.
            var opts = 0u;
            fd.GetOptions(out opts);
            opts |= (uint) DefaultFileOptions;
            fd.SetOptions(opts);
            
            // Set the window title to what the user wants.
            fd.SetTitle(this.Title);

            // File filters.
            var filters = new List<COMDLG_FILTERSPEC>();
            foreach (var fileType in this._acceptedTypes)
            {
                var filter = new COMDLG_FILTERSPEC();
                filter.pszName = fileType.Name;
                filter.pszSpec = "*." + fileType.Extension;
                filters.Add(filter);
            }

            if (!filters.Any() || AllowAnyFileType)
            {
                var filter = new COMDLG_FILTERSPEC();
                filter.pszName = "All files";
                filter.pszSpec = "*.*";
                filters.Add(filter);
            }
            
            // Set the filter list.
            fd.SetFileTypes((uint) filters.Count, filters.ToArray());
            fd.SetFileTypeIndex(0);

            // Set the initial directory.
            Win32SetInitialDirectory(fd);
            
            // Get the window handle for us. That way the dialog can disable input to it.
            var hwnd = Process.GetCurrentProcess().MainWindowHandle;

            // Show the dialog, get the dialog result.
            var hresult = fd.Show(hwnd);

            // Did everything go as planned?
            if (hresult == (uint) HRESULT.S_OK)
            {
                // Get the selected file.
                if (fd.GetResult(out var file) == (uint) HRESULT.S_OK)
                {
                    this._path = Win32GetShellPath(file);
                    result = FileOpenerResult.Ok;
                }
            }
            
            return result;
        }

        private string Win32GetShellPath(IShellItem shellItem)
        {
            if (shellItem.GetDisplayName(SIGDN_FILESYSPATH, out var ptr) == (uint) HRESULT.S_OK)
            {
                if (ptr != IntPtr.Zero)
                {
                    try
                    {
                        return Marshal.PtrToStringAuto(ptr);
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(ptr);
                    }
                }       
            }

            return string.Empty;
        }

        private FileOpenerResult Win32AskForFolder()
        {
            var hwnd = Process.GetCurrentProcess().MainWindowHandle;
            var clsid = ClsOpenFileDialog;
            var iid = ClsIFileDialog;

            CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var obj);

            var fd = (IFileDialog) obj;
            
            // Set the initial directory.
            Win32SetInitialDirectory(fd);

            // Set default dialog options for the file dialog.
            var opts = 0u;
            fd.GetOptions(out opts);
            opts |= (uint) DefaultFileOptions;
            opts |= (uint) FOS.FOS_PICKFOLDERS;
            fd.SetOptions(opts);

            // Set the title.
            fd.SetTitle(this.Title);
            
            // Thundershock result.
            var result = FileOpenerResult.Cancelled;

            if (fd.Show(hwnd) == (uint) HRESULT.S_OK)
            {
                if (fd.GetResult(out var dir) == (uint) HRESULT.S_OK)
                {
                    _path = Win32GetShellPath(dir);
                    result = FileOpenerResult.Ok;
                }
            }
            
            return result;
        }

        private void Win32SetInitialDirectory(IFileDialog fd)
        {
            // This is where we set the initial directory of the  file chooser.
            if (!string.IsNullOrWhiteSpace(InitialDirectory))
            {
                var iid = ClsIShellItem;
                if (SHCreateItemFromParsingName(this.InitialDirectory, IntPtr.Zero, ref iid, out var dirItem) ==
                    (uint) HRESULT.S_OK)
                {
                    fd.SetFolder(dirItem);
                    fd.SetDefaultFolder(dirItem);
                }
            }
        }
        
        #endregion
    }
}