using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace Petroware.Uom
{
  /// <summary>
  ///   Model a quantity (such as <em>length</em> or <em>acceleration</em>)
  ///   and its associated units.
  ///
  ///   This class is thread-safe.
  ///
  ///   \author <a href="mailto:info@petroware.no">Petroware AS</a>
  /// </summary>
  public sealed class Quantity
  {
    /// <summary>
    ///   Name of this quantity. Non-null.
    /// </summary>
    private readonly string name_;

    /// <summary>
    ///   Optional description. Null if none provided.
    /// </summary>
    private readonly string description_;

    /// <summary>
    ///   List of units for this quantity. Non-null.
    ///   The list may be empty, but if it's not, the first unit is
    ///   always the base unit. Access is protected by this.
    /// </summary>
    private readonly List<Unit> units_ = new List<Unit>();

    /// <summary>
    ///   Create a new quantity instance.
    /// </summary>
    ///
    /// <param name="name">
    ///   Name of quantity, such as "length". Non-null.
    /// </param>
    /// <param name="description">
    ///   Optional description. Null if none provided.
    /// </param>
    public Quantity(string name, string description)
    {
      Debug.Assert(name != null, "name cannot be null");

      name_ = name;
      description_ = description;
    }

    /// <summary>
    ///   Return name of this quantity.
    /// </summary>
    ///
    /// <returns>
    ///   Name of this quantity. Never null.
    /// </returns>
    public string GetName()
    {
      return name_;
    }

    /// <summary>
    ///   Return description of this quantity.
    /// </summary>
    ///
    /// <returns>
    ///   Description of this quantity. Null if none provided.
    /// </returns>
    public string GetDescription()
    {
      return description_;
    }

    /// <summary>
    ///   Return the units of this quantity. The first unit in the list
    ///   is always the base unit.
    /// </summary>
    ///
    /// <returns>
    ///   Units of this quantity. Never null.
    /// </returns>
    public IList<Unit> GetUnits()
    {
      lock (units_) {
        return units_.AsReadOnly();
      }
    }

    /// <summary>
    ///   Return the base unit of this quantity.
    ///   Equivalent to getUnits().get(0).
    /// </summary>
    ///
    /// <returns>
    ///   Base unit of this quantity, or null if no units has been added.
    /// </returns>
    public Unit GetBaseUnit()
    {
      lock (units_) {
        return units_.Count == 0 ? null : units_[0];
      }
    }

    /// <summary>
    ///   Associate the specified unit with this quantity.
    /// </summary>
    ///
    /// <param name="unit">
    ///   Unit to add. Non-null.
    /// </param>
    /// <param name="isBaseUnit">
    ///   True if this is the base unit, false otherwise.
    ///   If more than one unit is added as base unit, the
    ///   last one added will have this role. If no units are
    ///   added as base unit, the first unit added will have
    ///   this role.
    /// </param>
    /// <returns>
    ///
    /// </returns>
    public void AddUnit(Unit unit, bool isBaseUnit)
    {
      if (unit == null)
        throw new ArgumentNullException("unit");

      lock (units_) {
        units_.Insert(isBaseUnit ? 0 : units_.Count, unit);
      }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      StringBuilder s = new StringBuilder();
      s.Append("Name: " + name_ + "\n");
      if (description_ != null)
        s.Append("Description: " + description_ + "\n");
      foreach (Unit unit in units_)
        s.Append("  " + unit + "\n");

      return s.ToString();
    }
  }
}
