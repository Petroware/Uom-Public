using System;

namespace Petroware.Uom
{
  /// <summary>
  ///   Model a unit, such as "ft", "m", "N", "Hz", "m/s" etc., and
  ///   how values converts to the base unit of the same quantity.
  ///
  ///   This class is immutable.
  ///
  ///   \author <a href="mailto:info@petroware.no">Petroware AS</a>
  /// </summary>
  public sealed class Unit
  {
    /// <summary>
    ///   Name of unit such as "meter". Non-null.
    /// </summary>
    private readonly string name_;

    /// <summary>
    ///   Symbol of unit such as "m". Non-null.
    /// </summary>
    private readonly string symbol_;

    /// <summary>
    ///   Conversion factor a for converting to base unit.
    /// </summary>
    private readonly double a_;

    /// <summary>
    ///   Conversion factor b for converting to base unit.
    /// </summary>
    private readonly double b_;

    /// <summary>
    ///   Conversion factor c for converting to base unit.
    /// </summary>
    private readonly double c_;

    /// <summary>
    ///   Conversion factor d for converting to base unit.
    /// </summary>
    private readonly double d_;

    /// <summary>
    ///   Create a new unit.
    ///
    ///   The conversion consists of four factors that are applied as
    ///   follows when <em>value</em> is to be converted to base unit:
    ///
    ///   <pre>
    ///      base = (a * value + b) / (c * value + d);
    ///   </pre>
    ///
    ///   For most conversion only <em>a</em> is needed. In these cases
    ///   <em>b</em>, <em>c</em> and <em>d</em>
    ///   should be set to 0.0, 0.0 and 1.0 respectively. For units like
    ///   temperature a shift (<em>b</em>) is used as well, while c and d
    ///   is in practice never used.
    /// </summary>
    ///
    /// <param name="name">
    ///   Name of unit such as "meter". Non-null.
    /// </param>
    /// <param name="symbol">
    ///   Symbol of unit such as "m". Non-null.
    /// </param>
    /// <param name="a">
    ///   Conversion factor a for converting to base unit.
    /// </param>
    /// <param name="b">
    ///   Conversion factor b for converting to base unit.
    /// </param>
    /// <param name="c">
    ///   Conversion factor c for converting to base unit.
    /// </param>
    /// <param name="d">
    ///   Conversion factor d for converting to base unit.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   If name or symbol is null.
    /// </exception>
    public Unit(string name, string symbol, double a, double b, double c, double d)
    {
      if (name == null)
        throw new ArgumentNullException("name");

      if (symbol == null)
        throw new ArgumentNullException("symbol");

      name_ = name;
      symbol_ = symbol;
      a_ = a;
      b_ = b;
      c_ = c;
      d_ = d;
    }

    /// <summary>
    ///   Return name of this unit.
    /// </summary>
    /// <returns>
    ///   Name of this unit. Never null.
    /// </returns>
    public string GetName()
    {
      return name_;
    }

    /// <summary>
    ///   Return symbol of this unit.
    /// </summary>
    /// <returns>
    ///   Symbol of this unit. Never null.
    /// </returns>
    public string GetSymbol()
    {
      return symbol_;
    }

    /// <summary>
    ///   Convert the specified value to base unit in the quantity of this unit.
    /// </summary>
    /// <param name="value">
    ///   Value to convert.
    /// </param>
    /// <returns>
    ///   Value converted to base unit.
    /// </returns>
    public double ToBase(double value)
    {
      double baseValue = (a_ * value + b_) / (c_ * value + d_);
      return baseValue;
    }

    /// <summary>
    ///   Convert the specified value given in base unit to this unit.
    /// <param name="baseValue">
    ///   Base value to convert.
    /// </param>
    /// <returns>
    ///   Value converted to this unit.
    /// </returns>
    public double FromBase(double baseValue)
    {
      double value = (b_ - d_ * baseValue) / (c_ * baseValue - a_);
      return value;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      return  1 * name_.GetHashCode() +
              3 * symbol_.GetHashCode() +
              7 * a_.GetHashCode() +
             11 * b_.GetHashCode() +
             13 * c_.GetHashCode() +
             17 * d_.GetHashCode();
    }

    /// <inheritdoc/>
    public override bool Equals(object o)
    {
      if (o == this)
        return true;

      if (o == null)
        return false;

      Unit unit = o as Unit;

      if (unit == null)
        return false;

      if (!name_.Equals(unit.name_))
        return false;

      if (!symbol_.Equals(unit.symbol_))
        return false;

      if (a_ != unit.a_)
        return false;

      if (b_ != unit.b_)
        return false;

      if (c_ != unit.c_)
        return false;

      if (d_ != unit.d_)
        return false;

      return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      return name_ + " [" + symbol_ + "]";
    }
  }
}

