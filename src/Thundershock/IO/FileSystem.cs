using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Thundershock.IO
{
    public class FileSystem
    {
        private Node _root;

        private FileSystem(Node rootfs)
        {
            _root = rootfs;
        }

        private Node Resolve(string path)
        {
            var resolvedPath = PathUtils.Resolve(path);
            var parts = PathUtils.Split(resolvedPath);

            var node = _root;

            foreach (var part in parts)
            {
                if (node.CanList)
                {
                    var child = node.Children.FirstOrDefault(x => x.Name == part);
                    node = child;
                    if (node == null)
                        break;
                }
                else
                {
                    node = null;
                    break;
                }
            }
            
            return node;
        }

        public void CreateDirectory(string path)
        {
            var node = Resolve(path);
            if (node != null)
                throw new InvalidOperationException("File or directory exists.");

            var resolved = PathUtils.Resolve(path);
            var fname = PathUtils.GetFileName(resolved);
            var dname = PathUtils.GetDirectoryName(resolved);

            var dirnode = Resolve(dname);

            if (dirnode == null)
                throw new InvalidOperationException("Could not find a part of the path " + resolved + ".");

            if (dirnode.CanWrite)
                throw new InvalidOperationException("File exists.");

            if (!dirnode.CanCreate)
                throw new InvalidOperationException("Read-only filesystem.");

            dirnode.CreateDirectory(fname);
        }

        public void Delete(string path, bool recursive = false)
        {
            var node = Resolve(path);
            if (node == null)
                throw new InvalidOperationException("File or directory not found.");

            if (!node.CanDelete)
                throw new InvalidOperationException("Permission denied.");

            node.Delete(recursive);
        }

        public byte[] ReadAllBytes(string path)
        {
            var node = Resolve(path);
            if (node == null)
                throw new InvalidOperationException("File not found.");
            
            if (!node.CanRead)
                throw new InvalidOperationException("Is a directory.");

            using var s = node.Open(false);

            var arr = new byte[s.Length];
            s.Read(arr, 0, arr.Length);

            return arr;
        }
        
        public string ReadAllText(string path)
        {
            var arr = ReadAllBytes(path);

            return Encoding.UTF8.GetString(arr);
        }

        public Stream OpenFile(string path, bool append = false)
        {
            var node = Resolve(path);

            if (node != null)
            {
                if (node.CanWrite)
                {
                    return node.Open(append);
                }
                else throw new InvalidOperationException("Is a directory.");
            }
            else
            {
                var resolved = PathUtils.Resolve(path);
                var fname = PathUtils.GetFileName(resolved);
                var dirpath = PathUtils.GetDirectoryName(resolved);

                var dirNode = Resolve(dirpath);

                if (dirNode == null)
                    throw new InvalidOperationException("File or directory not found.");

                if (!dirNode.CanCreate)
                    throw new InvalidOperationException("Read only file system.");

                return dirNode.CreateFile(fname);
            }
        }
        
        public void WriteAllBytes(string path, byte[] bytes)
        {
            using var s = OpenFile(path);
            s.SetLength(0);
            s.Write(bytes, 0, bytes.Length);
        }

        public void WriteAllText(string path, string text)
        {
            WriteAllBytes(path, Encoding.UTF8.GetBytes(text));
        }
        
        public bool FileExists(string path)
        {
            var node = Resolve(path);
            return node != null && node.CanRead;
        }
        
        public bool DirectoryExists(string path)
        {
            var node = Resolve(path);
            return node != null && node.CanList;
        }

        public IEnumerable<string> GetFiles(string path)
        {
            var node = Resolve(path);

            if (node.CanList)
            {
                foreach (var child in node.Children)
                {
                    if (child.CanRead)
                    {
                        yield return PathUtils.Resolve(path, child.Name);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("File exists.");
            }
        }
        
        public IEnumerable<string> GetDirectories(string path)
        {
            var node = Resolve(path);

            if (node.CanList)
            {
                var resolved = PathUtils.Resolve(path);
                yield return PathUtils.Combine(resolved, PathUtils.CurrentDirectory);
                yield return PathUtils.Combine(resolved, PathUtils.ParentDirectory);
                
                
                foreach (var child in node.Children)
                {
                    if (child.CanList)
                    {
                        yield return PathUtils.Resolve(path, child.Name);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("File exists.");
            }
        }

        public static FileSystem FromHostDirectory(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(path);

            var hostNode = new HostDirectoryNode(null, path);

            return new FileSystem(hostNode);
        }
        
        public static FileSystem FromHostOS()
        {
            var node = null as Node;

            if (ThundershockPlatform.IsPlatform(Platform.Windows))
            {
                node = new WindowsMasterNode();
            }
            else
            {
                node = new UnixMasterNode();
            }
            
            return new FileSystem(node);
        }

        public static FileSystem FromNode(Node rootfs)
        {
            return new FileSystem(rootfs);
        }
    }
}