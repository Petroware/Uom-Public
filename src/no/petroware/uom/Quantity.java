package no.petroware.uom;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * Model a quantity (such as <em>length</em> or <em>acceleration</em>) and its
 * associated units.
 * <p>
 * This class is thread-safe.
 *
 * @author <a href="mailto:info@petroware.no">Petroware AS</a>
 */
public final class Quantity
{
  /** Name of this quantity. Non-null. */
  private final String name_;

  /** Optional description. Null if none provided. */
  private final String description_;

  /**
   * List of units for this quantity. Non-null.
   * The list may be empty, but if it's not, the first unit is
   * always the base unit. Access is protected by this.
   */
  private final List<Unit> units_ = new ArrayList<>();

  /**
   * Create a new quantity instance.
   *
   * @param name         Name of quantity, such as "length". Non-null.
   * @param description  Optional description. Null if none provided.
   * @throws IllegalArgumentException  If name is null.
   */
  public Quantity(String name, String description)
  {
    if (name == null)
      throw new IllegalArgumentException("name cannot be null");

    name_ = name;
    description_ = description;
  }

  /**
   * Return name of this quantity.
   *
   * @return  Name of this quantity. Never null.
   */
  public String getName()
  {
    return name_;
  }

  /**
   * Return description of this quantity.
   *
   * @return  Description of this quantity. Null if none provided.
   */
  public String getDescription()
  {
    return description_;
  }

  /**
   * Return the units of this quantity. The first unit in the list
   * is always the base unit.
   *
   * @return  Units of this quantity. Never null.
   */
  public synchronized List<Unit> getUnits()
  {
    return Collections.unmodifiableList(units_);
  }

  /**
   * Return the base unit of this quantity.
   * Equivalent to getUnits().get(0).
   *
   * @return  Base unit of this quantity, or null if no units has been added.
   */
  public synchronized Unit getBaseUnit()
  {
    return units_.isEmpty() ? null : units_.get(0);
  }

  /**
   * Associate the specified unit with this quantity.
   *
   * @param unit        Unit to add. Non-null.
   * @param isBaseUnit  True if this is the base unit, false otherwise.
   *                    If more than one unit is added as base unit, the
   *                    last one added will have this role. If no units are
   *                    added as base unit, the first unit added will have
   *                    this role.
   * @throws IllegalArgumentException  If unit is null.
   */
  public synchronized void addUnit(Unit unit, boolean isBaseUnit)
  {
    if (unit == null)
      throw new IllegalArgumentException("unit cannot be null");

    units_.add(isBaseUnit ? 0 : units_.size(), unit);
  }

  /** {@inheritDoc} */
  @Override
  public String toString()
  {
    StringBuilder s = new StringBuilder();
    s.append("Name: " + name_ + "\n");
    if (description_ != null)
      s.append("Description: " + description_ + "\n");
    for (Unit unit : units_)
      s.append("  " + unit + "\n");

    return s.toString();
  }
}
