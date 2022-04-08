﻿using GlueControl.Dtos;
using GlueControl.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Managers
{
    internal class GluxCommands
    {
        // nosOwner is needed until we have support for ObjectFinder, which requires the full GlueProjectSave
        public GeneralResponse CopyNamedObjectIntoElement(NamedObjectSave nos, GlueElement nosOwner, GlueElement targetElement, bool save = true)
        {
            // convert nos and target element to references
            var nosReference = new NamedObjectSaveReference();
            nosReference.NamedObjectName = nos.InstanceName;
            nosReference.GlueElementReference = new GlueElementReference();
            nosReference.GlueElementReference.ElementNameGlue = nosOwner.Name;

            var targetElementReference = new GlueElementReference();
            targetElementReference.ElementNameGlue = targetElement.Name;
            SendToGame(nameof(CopyNamedObjectIntoElement), nosReference, targetElementReference, save);

            // Until we get real 2 way communication working:
            return GeneralResponse.SuccessfulResponse;
        }


        private void SendToGame(string caller = null, params object[] parameters)
        {
            var dto = new GluxCommandDto();
            dto.Method = caller;
            dto.Parameters.AddRange(parameters);

            GlueControlManager.Self.SendToGlue(dto);
        }
    }
}
