﻿{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Entities;
using FlatRedBall.Graphics;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    public static class SelectionLogic
    {
        #region Fields/Properties

        static List<PositionedObject> tempPunchThroughList = new List<PositionedObject>();
        static Vector2 PushStartLocation;

        public static float? LeftSelect { get; private set; }
        public static float? RightSelect;
        public static float? TopSelect;
        public static float? BottomSelect;


        public static bool PerformedRectangleSelection { get; private set; }

        #endregion

        public static void GetInstanceOver(List<INameable> currentEntities, List<INameable> itemsOverToFill, List<ISelectionMarker> currentSelectionMarkers,
            bool punchThrough, ElementEditingMode elementEditingMode)
        {
            if (itemsOverToFill.Count > 0)
            {
                itemsOverToFill.Clear();
            }

            INameable objectOver = null;
            if (currentEntities.Count > 0 && punchThrough == false)
            {
                // Vic asks - why do we use the the current entities rather than the markers?
                var currentObjectOver = currentEntities.FirstOrDefault(item =>
                {
                    return item is PositionedObject asPositionedObject && IsCursorOver(item as PositionedObject);
                });
                if (currentObjectOver == null)
                {
                    var markerOver = currentSelectionMarkers.FirstOrDefault(item => item.IsCursorOverThis());
                    if (markerOver != null)
                    {
                        var index = currentSelectionMarkers.IndexOf(markerOver);
                        currentObjectOver = currentEntities[index];
                    }
                }
                objectOver = currentObjectOver;
            }

            if (punchThrough)
            {
                tempPunchThroughList.Clear();
            }

            IEnumerable<PositionedObject> availableItems = null;

            if (objectOver == null)
            {
                availableItems = GetAvailableObjects(elementEditingMode);

                if (availableItems != null)
                {
                    // here we sort every frame. This could be slow if we have a lot of objects so we may need to cache this somehow
                    foreach (PositionedObject objectAtI in availableItems.OrderByDescending(item => item.Z))
                    {
                        if (IsSelectable(objectAtI))
                        {
                            if (IsCursorOver(objectAtI))
                            {
                                if (punchThrough)
                                {
                                    tempPunchThroughList.Add(objectAtI);
                                }
                                else
                                {
                                    objectOver = objectAtI;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (punchThrough)
            {
                if (tempPunchThroughList.Count == 0)
                {
                    objectOver = null;
                }
                else if (tempPunchThroughList.Count == 1)
                {
                    objectOver = tempPunchThroughList[0];
                }
                else if (tempPunchThroughList.Any(item => currentEntities.Contains(item)) == false)
                {
                    // just pick the first
                    objectOver = tempPunchThroughList[0];
                }
                else
                {
                    var index = tempPunchThroughList.IndexOf(currentEntities.FirstOrDefault() as PositionedObject);
                    if (index < tempPunchThroughList.Count - 1)
                    {
                        objectOver = tempPunchThroughList[index + 1];
                    }
                    else
                    {
                        objectOver = tempPunchThroughList[0];
                    }
                }
            }

            if (PerformedRectangleSelection && availableItems != null)
            {
                foreach (var item in availableItems)
                {
                    if (IsSelectable(item) && IsRectangleSelectionOver(item))
                    {
                        itemsOverToFill.Add(item);
                    }
                }
            }

            if (objectOver != null)
            {
                itemsOverToFill.Add(objectOver);
            }
        }

        internal static void DoDragSelectLogic()
        {
            var cursor = GuiManager.Cursor;

            PerformedRectangleSelection = false;

            if (cursor.PrimaryDown == false && !cursor.PrimaryClick)
            {
                LeftSelect = null;
                RightSelect = null;
                TopSelect = null;
                BottomSelect = null;
            }

            if (cursor.PrimaryPush)
            {
                PushStartLocation = cursor.WorldPosition;
                LeftSelect = null;
                RightSelect = null;
                TopSelect = null;
                BottomSelect = null;
            }
            if (cursor.PrimaryDown)
            {
                LeftSelect = Math.Min(PushStartLocation.X, cursor.WorldX);
                RightSelect = Math.Max(PushStartLocation.X, cursor.WorldX);

                TopSelect = Math.Max(PushStartLocation.Y, cursor.WorldY);
                BottomSelect = Math.Min(PushStartLocation.Y, cursor.WorldY);

                var centerX = (LeftSelect.Value + RightSelect.Value) / 2.0f;
                var centerY = (TopSelect.Value + BottomSelect.Value) / 2.0f;

                var width = RightSelect.Value - LeftSelect.Value;
                var height = TopSelect.Value - BottomSelect.Value;

                Color selectionColor = Color.LightBlue;

                EditorVisuals.Rectangle(width, height, new Vector3(centerX, centerY, 0), selectionColor);
            }
            if (cursor.PrimaryClick)
            {
                // get all things within this rect...
                PerformedRectangleSelection = LeftSelect != RightSelect && TopSelect != BottomSelect;
            }
        }

        public static IEnumerable<PositionedObject> GetAvailableObjects(ElementEditingMode elementEditingMode)
        {
            IEnumerable<PositionedObject> availableItems = null;

            if (elementEditingMode == ElementEditingMode.EditingScreen)
            {
                // is it slow to do this every frame?
                availableItems = SpriteManager.ManagedPositionedObjects
                    // We check for null parents so we don't grab an object that is embedded inside an entity instance.
                    .Where(item => item is CameraControllingEntity == false && item.Parent == null)
                    .Concat(SpriteManager.AutomaticallyUpdatedSprites.Where(item => item.Parent == null))
                    .Concat(TextManager.AutomaticallyUpdatedTexts.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisibleRectangles.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisibleCircles.Where(item => item.Parent == null))
                    .Concat(ShapeManager.VisiblePolygons.Where(item => item.Parent == null))
                    ;
            }
            else if (elementEditingMode == ElementEditingMode.EditingEntity)
            {
                var screen = FlatRedBall.Screens.ScreenManager.CurrentScreen as Screens.EntityViewingScreen;
                var entity = (screen.CurrentEntity as PositionedObject);
                if (entity != null)
                {
                    availableItems = entity.Children;
                }
                else if (SpriteManager.ManagedPositionedObjects.Count > 0)
                {
                    availableItems = SpriteManager.ManagedPositionedObjects[0].Children;
                }
            }

            return availableItems;
        }

        public static bool IsSelectable(INameable nameable)
        {
#if SupportsEditMode
            if(nameable is PositionedObject positionedObject)
            {
                return positionedObject.CreationSource == "Glue" && 
                    (positionedObject is FlatRedBall.TileGraphics.LayeredTileMap) == false ;
            }
            else
            {
                return nameable is FlatRedBall.TileCollisions.TileShapeCollection;
            }
#else
            return false;
#endif
        }

        private static bool IsCursorOver(PositionedObject objectAtI)
        {
            var cursor = GuiManager.Cursor;
            var worldX = cursor.WorldXAt(objectAtI.Z);
            var worldY = cursor.WorldYAt(objectAtI.Z);

            GetDimensionsFor(objectAtI, out float minX, out float maxX, out float minY, out float maxY);

            return
                objectAtI.Z < (Camera.Main.Z - Camera.Main.NearClipPlane) &&
                worldX >= minX &&
                worldX <= maxX &&
                worldY >= minY &&
                worldY <= maxY;
        }

        private static bool IsRectangleSelectionOver(PositionedObject item)
        {
            GetDimensionsFor(item, out float minX, out float maxX, out float minY, out float maxY);

            return RightSelect != null &&
                    RightSelect.Value >= minX &&
                    LeftSelect.Value <= maxX &&
                    TopSelect.Value >= minY &&
                    BottomSelect.Value <= maxY;
        }

        #region Get Dimensions

        internal static void GetDimensionsFor(PositionedObject itemOver,
            out float minX, out float maxX, out float minY, out float maxY)
        {
            // We used to use the position as part of the min and max bounds, but this causes problems
            // if some objects are only visible when the cursor is over them. Therefore, always use half dimension
            // width for selection:
            minX = itemOver.X;
            maxX = itemOver.X;
            minY = itemOver.Y;
            maxY = itemOver.Y;
            GetDimensionsForInner(itemOver, ref minX, ref maxX, ref minY, ref maxY);

            float minDimension = 0;

            // if it's scalable, then we should show the 
            // bounds exactly as is, so it can be resized.
            // Otherwise, give it a min dimension in case it's
            // empty or really small.
            var isScalable = itemOver is IReadOnlyScalable;
            if (!isScalable)
            {
                var multiplier = Camera.Main.OrthogonalHeight / Camera.Main.DestinationRectangle.Height;
                minDimension = 16 * multiplier;
            }

            if (maxX - minX < minDimension)
            {
                var extraToAdd = minDimension - (maxX - minX);

                minX -= extraToAdd / 2.0f;
                maxX += extraToAdd / 2.0f;
            }

            if (maxY - minY < minDimension)
            {
                var extraToAdd = minDimension - (maxY - minY);

                minY -= extraToAdd / 2.0f;
                maxY += extraToAdd / 2.0f;
            }


        }

        private static void GetDimensionsForInner(PositionedObject itemOver,
            ref float minX, ref float maxX, ref float minY, ref float maxY)
        {
            if (itemOver is IMinMax minMax)
            {
                minX = minMax.MinXAbsolute;
                maxX = minMax.MaxXAbsolute;

                minY = minMax.MinYAbsolute;
                maxY = minMax.MaxYAbsolute;
            }
            else if (itemOver is IReadOnlyScalable asScalable)
            {
                minX = Math.Min(minX, itemOver.X - asScalable.ScaleX);
                maxX = Math.Max(maxX, itemOver.X + asScalable.ScaleX);

                minY = Math.Min(minY, itemOver.Y - asScalable.ScaleY);
                maxY = Math.Max(maxY, itemOver.Y + asScalable.ScaleY);
            }
            else if (itemOver is Circle asCircle)
            {
                minX = Math.Min(minX, itemOver.X - asCircle.Radius);
                maxX = Math.Max(maxX, itemOver.X + asCircle.Radius);

                minY = Math.Min(minY, itemOver.Y - asCircle.Radius);
                maxY = Math.Max(maxY, itemOver.Y + asCircle.Radius);
            }
            else if (itemOver is Line asLine)
            {
                minX = Math.Min(minX, asLine.X + (float)asLine.RelativePoint1.X);
                maxX = Math.Max(maxX, asLine.X + (float)asLine.RelativePoint1.X);

                minY = Math.Min(minY, asLine.Y - (float)asLine.RelativePoint1.Y);
                maxY = Math.Max(maxY, asLine.Y + (float)asLine.RelativePoint1.Y);

                minX = Math.Min(minX, asLine.X + (float)asLine.RelativePoint2.X);
                maxX = Math.Max(maxX, asLine.X + (float)asLine.RelativePoint2.X);

                minY = Math.Min(minY, asLine.Y - (float)asLine.RelativePoint2.Y);
                maxY = Math.Max(maxY, asLine.Y + (float)asLine.RelativePoint2.Y);
            }
#if HasGum
            else if(itemOver is GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper gumWrapper)
            {
                var gue = gumWrapper.GumObject;

                var absoluteOrigin = gumWrapper.GetAbsolutePositionInFrbSpace(gue);

                // assume top left origin for now
                minX = Math.Min(minX, absoluteOrigin.X - gue.GetAbsoluteWidth()/2.0f);
                maxX = Math.Max(maxX, absoluteOrigin.X + gue.GetAbsoluteWidth()/2.0f);

                minY = Math.Min(minY, absoluteOrigin.Y - gue.GetAbsoluteHeight()/2.0f);
                maxY = Math.Max(maxY, absoluteOrigin.Y + gue.GetAbsoluteHeight() / 2.0f);
            }

#endif
            else if (itemOver is Polygon polygon)
            {
                if (polygon.Points != null)
                {
                    for (int i = 0; i < polygon.Points.Count; i++)
                    {
                        var absolute = polygon.AbsolutePointPosition(i);

                        minX = Math.Min(minX, absolute.X);
                        maxX = Math.Max(maxX, absolute.X);

                        minY = Math.Min(minY, absolute.Y);
                        maxY = Math.Max(maxY, absolute.Y);
                    }
                }
            }
            else if (itemOver is Text text)
            {
                if (text.HorizontalAlignment == HorizontalAlignment.Left)
                {
                    minX = Math.Min(minX, text.X);
                    maxX = Math.Max(maxX, text.X + text.Width);
                }
                else if (text.HorizontalAlignment == HorizontalAlignment.Center)
                {
                    minX = Math.Min(minX, text.X - text.Width / 2.0f);
                    maxX = Math.Max(maxX, text.X + text.Width / 2.0f);
                }
                else // right
                {
                    minX = Math.Min(minX, text.X - text.Width);
                    maxX = Math.Max(maxX, text.X);
                }

                if (text.VerticalAlignment == VerticalAlignment.Top)
                {
                    minY = Math.Min(minY, text.Y - text.Height);
                    maxY = Math.Max(maxY, text.Y);
                }
                else if (text.VerticalAlignment == VerticalAlignment.Center)
                {
                    minY = Math.Min(minY, text.Y - text.Height / 2.0f);
                    maxY = Math.Max(maxY, text.Y + text.Height / 2.0f);
                }
                else // bottom
                {
                    minY = Math.Min(minY, text.Y);
                    maxY = Math.Max(maxY, text.Y + text.Height);
                }
            }
            else
            {
                for (int i = 0; i < itemOver.Children.Count; i++)
                {
                    var child = itemOver.Children[i];

                    var shouldConsiderChild = true;

                    if (child is IVisible asIVisible)
                    {
                        shouldConsiderChild = asIVisible.Visible;
                    }

                    if (shouldConsiderChild)
                    {
                        GetDimensionsForInner(child, ref minX, ref maxX, ref minY, ref maxY);
                    }
                }
            }
        }

        #endregion

    }

    #region IMinMax

    public interface IMinMax
    {
        float MinXAbsolute { get; }
        float MaxXAbsolute { get; }
        float MinYAbsolute { get; }
        float MaxYAbsolute { get; }
    }

    #endregion

}
