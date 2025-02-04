#define PreVersion
#define HasFormsObject
#define AddedGeneratedGame1
#define REFERENCES_FRB_SOURCE


using FlatRedBall.Math.Geometry;
using FlatRedBall.TileCollisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Math.Collision
{
    public class CollidableListVsTileShapeCollectionRelationship<FirstCollidableT> :
        CollisionRelationship
        where FirstCollidableT : PositionedObject, ICollidable
    {
        CollidableVsTileShapeCollectionData<FirstCollidableT> data;

        public void SetFirstSubCollision(Func<FirstCollidableT, Circle> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionCircle = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, AxisAlignedRectangle> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionRectangle = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Polygon> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionPolygon = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, Line> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionLine = subCollisionFunc; data.FirstSubObjectName = subObjectName; }
        public void SetFirstSubCollision(Func<FirstCollidableT, ICollidable> subCollisionFunc, string subObjectName = null) { data.firstSubCollisionCollidable = subCollisionFunc; data.FirstSubObjectName = subObjectName; }

        public Action<FirstCollidableT, TileShapeCollection> CollisionOccurred;

        PositionedObjectList<FirstCollidableT> list;

        public override object FirstAsObject => list;
        public override object SecondAsObject => data.TileShapeCollection;

        public CollidableListVsTileShapeCollectionRelationship(PositionedObjectList<FirstCollidableT> list, TileShapeCollection tileShapeCollection)
        {
            SetTileShapeCollection(tileShapeCollection);
            this.list = list;
        }

        /// <summary>
        /// Sets the TileShapeCollection used by this CollisionRelationship. This is automatically called by the constructor, so it should only be manually called to change the TileShapeCollection after this relationship has been created.
        /// </summary>
        /// <param name="tileShapeCollection"></param>
        public void SetTileShapeCollection(TileShapeCollection tileShapeCollection)
        {
            data = new CollidableVsTileShapeCollectionData<FirstCollidableT>(tileShapeCollection);
        }

        public override bool DoCollisions()
        {
            bool didCollisionOccur = false;
            if (skippedFrames < FrameSkip)
            {
                skippedFrames++;
            }
            else
            {
                bool isSupported = CollisionLimit == CollisionLimit.All;

                if (CollisionLimit == CollisionLimit.First)
                {
                    string message = $"{nameof(CollidableVsTileShapeCollectionRelationship<FirstCollidableT>)} does not implement CollisionLimit {CollisionLimit}";
                    throw new NotImplementedException();
                }

                if (CollisionLimit == CollisionLimit.Closest)
                {
                    // we actually only support closest if it's a line subcollision. Otherwise throw an exception
                    if (data.firstSubCollisionLine != null)
                    {
                        // it's okay
                    }
                    else
                    {
                        string message = $"{nameof(CollidableVsTileShapeCollectionRelationship<FirstCollidableT>)} does not implement CollisionLimit {CollisionLimit}";
                        throw new NotImplementedException();
                    }
                }

                skippedFrames = 0;

                for (int i = list.Count - 1; i > -1; i--)
                {
                    var singleObject = list[i];

#if CollisionRelationshipManualPhysics
                    bool didCollide = DoCollisionPhysicsInner(singleObject, ArePhysicsAppliedAutomatically == false);
#else
                    bool didCollide = DoCollisionPhysicsInner(singleObject, false);
#endif

                    if (didCollide)
                    {
                        didCollisionOccur = true;
                        CollisionOccurred?.Invoke(singleObject, data.TileShapeCollection);

#if ICollidableHasItemsCollidedAgainst || REFERENCES_FRB_SOURCE
                        singleObject.ItemsCollidedAgainst.Add(data.TileShapeCollection.Name);
#endif
#if ICollidableHasObjectsCollidedAgainst || REFERENCES_FRB_SOURCE
                        singleObject.ObjectsCollidedAgainst.Add(data.TileShapeCollection);
#endif
                    }
                }
            }
            return didCollisionOccur;
        }

        public bool DoCollisionPhysics(FirstCollidableT singleObject)
        {
            return DoCollisionPhysicsInner(singleObject, false);
        }

        private bool DoCollisionPhysicsInner(FirstCollidableT singleObject, bool eventOnly)
        {
            bool didCollide;
            // todo - tile shape collections need to report their deep collision, they don't currently:
            if (CollisionType == CollisionType.EventOnlyCollision || eventOnly)
            {
                didCollide = data.CollideAgainstConsiderSubCollisionEventOnly(singleObject, CollisionLimit);
            }
            else if (CollisionType == CollisionType.MoveCollision)
            {
                didCollide = data.CollideAgainstConsiderSubCollisionMove(singleObject);
            }
            else if (CollisionType == CollisionType.BounceCollision)
            {
                didCollide = data.CollideAgainstConsiderSubCollisionBounce(singleObject, bounceElasticity);
            }
#if CollisionRelationshipsSupportMoveSoft
            else if (CollisionType == CollisionType.MoveSoftCollision)
            {
                throw new NotImplementedException("soft collision against tile shape collections is not currently supported");
            }
#endif
            else
            {
                throw new NotImplementedException();
            }

            return didCollide;
        }
    }
}
