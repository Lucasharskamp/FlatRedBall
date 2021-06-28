﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public interface IFileReferencer
    {
        bool UseGlobalContent
        {
            get;
            set;
        }

        ReferencedFileSave GetReferencedFileSave(string name);


        List<ReferencedFileSave> ReferencedFiles
        {
            get;
        }
    }

    
    public static class FileReferencerHelper
    {
        public static ReferencedFileSave GetReferencedFileSave(IFileReferencer instance, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Replace("\\", "/");
                foreach (ReferencedFileSave rfs in instance.ReferencedFiles)
                {
                    if (rfs.Name == name)
                    {
                        return rfs;
                    }
                }
                // didn't find it, so let's try un-qualified
                if(name.Contains('/') == false)
                {
                    foreach (ReferencedFileSave rfs in instance.ReferencedFiles)
                    {
                        var nameNoExtension = IO.FileManager.RemoveExtension(rfs.Name);
                        if (nameNoExtension.EndsWith('/' + name))
                        {
                            return rfs;
                        }
                    }
                }
            }
            return null;
        }
    }
}
