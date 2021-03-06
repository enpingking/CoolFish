﻿using System;
using System.Collections.Specialized;
using CoolFishNS.Management.CoolManager.Objects.Structs;

namespace CoolFishNS.Management.CoolManager.Objects
{
    /// <summary>
    ///     An Object. Everything that inherits from WoWObject, such as an item, is also an object.
    ///     But an object is not necessarily an item.
    /// </summary>
    public class WoWObject
    {
        /// <summary>
        ///     Instantiates a new WoWObject.
        /// </summary>
        /// <param name="baseAddress">The Object's Base Address.</param>
        public WoWObject(IntPtr baseAddress)
        {
            BaseAddress = baseAddress;
        }

        /// <summary>
        ///     The Base Address of the object.
        /// </summary>
        public IntPtr BaseAddress { get; set; }

        /// <summary>
        ///     The object's GUID.
        /// </summary>
        public virtual WoWGUID Guid
        {
            get { return BotManager.Memory.Read<WoWGUID>((BaseAddress + (int) Offsets.WowObject.VisibleGuid)); }
        }

        /// <summary>
        ///     The object's Data.
        /// </summary>
        public short Data
        {
            get { return GetStorageField<short>(Offsets.WoWObjectFields.Data); }
        }

        /// <summary>
        ///     The object's Type.
        /// </summary>
        public ObjectManager.ObjectType Type
        {
            get { return BotManager.Memory.Read<ObjectManager.ObjectType>(BaseAddress + (int) Offsets.WowObject.Type); }
        }

        /// <summary>
        ///     The object's Entry.
        /// </summary>
        public uint Entry
        {
            get { return GetStorageField<uint>(Offsets.WoWObjectFields.EntryID); }
        }

        /// <summary>
        ///     Returns the object's X position.
        /// </summary>
        public virtual float X
        {
            get { return BotManager.Memory.Read<float>(BaseAddress + (int) Offsets.WowObject.X); }
        }

        /// <summary>
        ///     Returns the object's Y position.
        /// </summary>
        public virtual float Y
        {
            get { return BotManager.Memory.Read<float>(BaseAddress + (int) Offsets.WowObject.Y); }
        }

        /// <summary>
        ///     Returns the object's Z position.
        /// </summary>
        public virtual float Z
        {
            get { return BotManager.Memory.Read<float>(BaseAddress + (int) Offsets.WowObject.Z); }
        }

        public Point Location
        {
            get { return new Point(X, Y, Z); }
        }

        /// <summary>
        ///     The distance.
        /// </summary>
        public float Distance
        {
            get { return (float) Point.Distance(ObjectManager.Me.Location, Location); }
        }

        /// <summary>
        ///     Returns the Facing orientation.
        /// </summary>
        public virtual float Facing
        {
            get { return BotManager.Memory.Read<float>(BaseAddress + (int) Offsets.WowObject.Rotation); }
        }

        /// <summary>
        ///     Determines if the unit is our local player.
        /// </summary>
        public bool IsMe
        {
            get { return Guid.Equals(ObjectManager.Me.Guid); }
        }

        /// <summary>
        ///     Returns the Dynamic Flags.
        /// </summary>
        public virtual BitVector32 DynamicFlags
        {
            get { return GetStorageField<BitVector32>(Offsets.WoWObjectFields.DynamicFlags); }
        }

        /// <summary>
        ///     OBJECT_FIELD_SCALE_X
        /// </summary>
        public int Scale_X
        {
            get { return GetStorageField<int>(Offsets.WoWObjectFields.Scale); }
        }

        /// <summary>
        ///     Move the Active Player to this objects current location
        ///     Note: Click To Move MUST be enabled in game for this to work!
        /// </summary>
        public void MoveTo()
        {
            ObjectManager.Me.MoveTo(Location);
        }

        public static WoWGameObject ToWoWGameObject(WoWObject obj)
        {
            return new WoWGameObject(obj.BaseAddress);
        }

        #region <Storage Field Methods>

        /// <summary>
        ///     Gets the descriptor struct.
        /// </summary>
        /// <typeparam name="T">struct</typeparam>
        /// <param name="field">Descriptor field</param>
        /// <returns>Descriptor field</returns>
        protected T GetStorageField<T>(int field) where T : struct
        {
            var m_pStorage = BotManager.Memory.Read<IntPtr>(BaseAddress + 0x4);
            return BotManager.Memory.Read<T>((m_pStorage + field));
        }

        /// <summary>
        ///     Gets the descriptor struct.
        ///     Overload for when not casting uint.
        /// </summary>
        /// <typeparam name="T">struct</typeparam>
        /// <param name="field">Descriptor field</param>
        /// <returns>Descriptor field</returns>
        protected T GetStorageField<T>(Offsets.WoWObjectFields field) where T : struct
        {
            return GetStorageField<T>((int) field);
        }

        #endregion <Storage Field Methods>
    }
}