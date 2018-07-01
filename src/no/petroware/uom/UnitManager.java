package no.petroware.uom;

import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Properties;
import java.util.Set;
import java.util.concurrent.CopyOnWriteArrayList;

import org.w3c.dom.Document;
import org.w3c.dom.Element;
import org.xml.sax.SAXException;

/**
 * Units of measurement manager.
 * <p>
 * Singleton instance and main access point for quantities, units and
 * unit conversions.
 * <p>
 * This class is thread-safe.
 *
 * @author <a href="mailto:info@petroware.no">Petroware AS</a>
 */
public final class UnitManager
{
  /** The XML file with the Energistics unit database. */
  private final static String UNITS_FILE = "witsmlUnitDict-2.2.xml";

  /** Property file holding unit aliases. */
  private final static String UNIT_ALIASES_FILE = "unit_aliases.txt";

  /** Property file holding display symbols. */
  private final static String DISPLAY_SYMBOLS_FILE = "display_symbols.txt";

  /** The sole instance of this class. */
  private final static UnitManager instance_ = new UnitManager();

  /** Mapping between unit symbol alias and their equivalent "official" unit symbol. */
  private final Properties unitAliases_ = new Properties();

  /** Mapping between unit symbol and its equivalent display symbol. */
  private final Properties displaySymbols_ = new Properties();

  /** Quantities known by this manager. */
  private final List<Quantity> quantities_ = new CopyOnWriteArrayList<>();

  /**
   * Return the sole instance of this class.
   *
   * @return  The sole instance of this class. Never null.
   */
  public static UnitManager getInstance()
  {
    return instance_;
  }

  /**
   * Create a unit manager instance.
   */
  private UnitManager()
  {
    loadEnergisticsQuantities();
    loadUnitAliases();
    loadDisplaySymbols();
  }

  /**
   * Add specified alias to be associated with the given official
   * unit symbol.
   * <p>
   * The alias will be used when identifying Unit instances
   * from unit symbols and affects all methods of this class taking
   * unit symbol as argument.
   * <p>
   * Multiple aliases can be added for each unit symbol.
   *
   * @param unitSymbolAlias  Alias to add. Non-null.
   * @param unitSymbol       Unit symbol to associated alias with. Non-null.
   * @throws IllegalArgumentException  If unitSymbolAlias or unitSymbol is null.
   */
  public void addUnitAlias(String unitSymbolAlias, String unitSymbol)
  {
    if (unitSymbolAlias == null)
      throw new IllegalArgumentException("unitSymbolAlias cannot be null");

    if (unitSymbol == null)
      throw new IllegalArgumentException("unitSymbol cannot be null");

    unitAliases_.setProperty(unitSymbolAlias.toLowerCase(), unitSymbol);
  }

  /**
   * Set the specified display symbol for the given unit symbol.
   *
   * @param unitSymbol     Official unit symbol. Non-null.
   * @param displaySymbol  Associated display symbol. Non-null.
   * @throws IllegalArgumentException  If unitSymbol or displaySymbol is null.
   */
  public void setDisplaySymbol(String unitSymbol, String displaySymbol)
  {
    if (unitSymbol == null)
      throw new IllegalArgumentException("unitSymbol cannot be null");

    if (displaySymbol == null)
      throw new IllegalArgumentException("displaySymbol cannot be null");

    displaySymbols_.setProperty(unitSymbol, displaySymbol);
  }

  /**
   * Set the specified display symbol for the given unit.
   *
   * @param unit           Unit to set display symbol of. Non-null.
   * @param displaySymbol  Associated display symbol. Non-null.
   * @throws IllegalArgumentException  If unitSymbol or displaySymbol is null.
   */
  public void setDisplaySymbol(Unit unit, String displaySymbol)
  {
    if (unit == null)
      throw new IllegalArgumentException("unit cannot be null");

    if (displaySymbol == null)
      throw new IllegalArgumentException("displaySymbol cannot be null");

    setDisplaySymbol(unit.getSymbol(), displaySymbol);
  }

  /**
   * Return all quantities known by this unit manager.
   *
   * @return  All quantities. Never null.
   */
  public List<Quantity> getQuantities()
  {
    return Collections.unmodifiableList(quantities_);
  }

  /**
   * Add the specified quantity to this unit manager.
   *
   * @param quantity  Quantity to add. Non-null.
   * @throws IllegalArgumentException  If quantity is null or already contained
   *                  in this manager.
   */
  public void addQuantity(Quantity quantity)
  {
    if (quantity == null)
      throw new IllegalArgumentException("quantity cannot be null");

    if (findQuantity(quantity.getName()) != null)
      throw new IllegalArgumentException("Quantity is already present: " + quantity.getName());

    quantities_.add(quantity);
  }

  /**
   * Find quantity of the given name.
   *
   * @param quantityName  Name of quantity to find. Non-null.
   * @return              Requested quantity or null if not found.
   * @throws IllegalArgumentException  If quantityName is null.
   */
  public Quantity findQuantity(String quantityName)
  {
    if (quantityName == null)
      throw new IllegalArgumentException("quantityName cannot be null");

    for (Quantity quantity : quantities_) {
      if (quantity.getName().equals(quantityName))
        return quantity;
    }

    return  null;
  }

  /**
   * Find corresponding unit instance for the given unit symbol.
   * <p>
   * The alias mapping is considered, and units are searched both case
   * sensitive and case insensitive.
   *
   * @param unitSymbol  Unit symbol to find unit for. May be null for unitless.
   * @return            Associated unit, or null if not found.
   */
  public Unit findUnit(String unitSymbol)
  {
    if (unitSymbol == null || unitSymbol.trim().isEmpty())
      unitSymbol = "unitless";

    String lowerCase = unitSymbol.toLowerCase(Locale.US).trim();

    // Check if there is an explicit mapping
    String actualUnitSymbol = unitAliases_.getProperty(lowerCase);
    if (actualUnitSymbol != null)
      unitSymbol = actualUnitSymbol;

    // Loop over all units to see if there is a matching one
    // with same case
    for (Quantity quantity : quantities_) {
      for (Unit unit : quantity.getUnits()) {
        String symbol = unit.getSymbol();
        if (unitSymbol.equals(symbol))
          return unit;
      }
    }

    // Do the same, but case insensitive this time
    for (Quantity quantity : quantities_) {
      for (Unit unit : quantity.getUnits()) {
        String symbol = unit.getSymbol();
        String symbolLowerCase = symbol.toLowerCase(Locale.US);
        if (lowerCase.equals(symbolLowerCase))
          return unit;
      }
    }

    // Not found
    return null;
  }

  /**
   * Return all units that are convertible with the specified unit.
   *
   * @param unit  Unit to consider. Non-null.
   * @return      All convertible units. Never null.
   * @throws IllegalArgumentException  If unit is null.
   */
  public List<Unit> findConvertibleUnits(Unit unit)
  {
    if (unit == null)
      throw new IllegalArgumentException("unit cannot be null");

    Set<Unit> units = new HashSet<>();

    for (Quantity quantity : quantities_) {
      if (quantity.getUnits().contains(unit))
        units.addAll(quantity.getUnits());
    }

    units.remove(unit);

    return new ArrayList<Unit>(units);
  }

  /**
   * Return all units that are convertible with the unit of the specified
   * symbol.
   *
   * @param unitSymbol  Symbol of unit to consider. Null if unitless.
   * @return            All convertible units. Never null.
   */
  public List<Unit> findConvertibleUnits(String unitSymbol)
  {
    Unit unit = findUnit(unitSymbol);
    return unit != null ? findConvertibleUnits(unit) : new ArrayList<Unit>();
  }

  /**
   * Return all quantities that includes the specified unit.
   *
   * @param unit  Unit to consider. Non-null.
   * @return      Requested quantities. Never null.
   * @throws IllegalArgumentException  If unit is null.
   */
  public List<Quantity> findQuantities(Unit unit)
  {
    if (unit == null)
      throw new IllegalArgumentException("unit cannot be null");

    List<Quantity> quantities = new ArrayList<>();

    for (Quantity quantity : quantities_) {
      if (quantity.getUnits().contains(unit))
        quantities.add(quantity);
    }

    // If (one of) the quantities contains the euclid unit (unitless)
    // we add the dimensionless quantity as well.
    Unit euclidUnit = findUnit("Euc");
    for (Quantity quantity : quantities) {
      if (quantity.getUnits().contains(euclidUnit)) {
        quantities.add(findQuantity("dimensionless"));
        break;
      }
    }

    return quantities;
  }

  /**
   * Return all quantities that includes the unit of the specified symbol.
   *
   * @param unitSymbol  Unit symbol of unit to consider. Null if unitless.
   * @return            Requested quantities. Never null.
   */
  public List<Quantity> findQuantities(String unitSymbol)
  {
    Unit unit = findUnit(unitSymbol);
    return unit != null ? findQuantities(unit) : new ArrayList<Quantity>();
  }

  /**
   * Find quantity of the specified unit.
   * <p>
   * Note that a unit may be contained in multiple quantities.
   * This method is convenient if the client knows that the unit
   * exists in one quantity only. If it exists in more than one
   * quantity, the first one encountered is returned.
   *
   * @param unit  Unit to consider. Non-null.
   * @return      Requested quantity or null if none found.
   * @throws IllegalArgumentException  If unit is null.
   */
  public Quantity findQuantity(Unit unit)
  {
    if (unit == null)
      throw new IllegalArgumentException("unit cannot be null");

    List<Quantity> quantities = findQuantities(unit);

    if (quantities.isEmpty())
      return null;

    else if (quantities.size() == 1)
      return quantities.get(0);

    else {
      // Unit clash: "Siemens (S) - seconds (s)"
      if (quantities.contains(findQuantity("time")))
        return findQuantity("time");

      // TODO: Others

      return quantities.get(0);
    }
  }

  /**
   * Check if it is possible to convert between the two specified units.
   *
   * @param unit1  First unit to consider. Non-null.
   * @param unit2  Second unit to consider. Non-null.
   * @return       True if it is possible to convert between the two,
   *               false otherwise.
   * @throws IllegalArgumentException  If unit1 or unit2 is null.
   */
  public boolean canConvert(Unit unit1, Unit unit2)
  {
    if (unit1 == null)
      throw new IllegalArgumentException("unit1 cannot be null");

    if (unit2 == null)
      throw new IllegalArgumentException("unit2 cannot be null");

    List<Quantity> quantities1 = findQuantities(unit1);
    List<Quantity> quantities2 = findQuantities(unit2);

    for (Quantity quantity : quantities1)
      if (quantities2.contains(quantity))
        return true;

    return false;
  }

  /**
   * Check if it is possible to convert between the two specified units.
   *
   * @param unitSymbol1  Unit symbol of first unit to consider. Null if unitless.
   * @param unitSymbol2  Unit symbol of second unit to consider. Null if unitless.
   * @return             True if it is possible to convert between the two,
   *                     false otherwise.
   */
  public boolean canConvert(String unitSymbol1, String unitSymbol2)
  {
    if (unitSymbol1 == null || unitSymbol2 == null)
      return false;

    Unit unit1 = findUnit(unitSymbol1);
    Unit unit2 = findUnit(unitSymbol2);

    return unit1 != null && unit2 != null ? canConvert(unit1, unit2) : false;
  }

  /**
   * Convert the specified value between the two given units.
   * <p>
   * Note that it is the client responsibility to check if it makes sense to
   * convert between the given units. This method simply converts the value
   * to base of the from unit, and convert this result from base of the to unit,
   * without considering the compatibility between the two.
   *
   * @param fromUnit  Current unit of value. Non-null.
   * @param toUnit    Unit to convert to. Non-null.
   * @param value     Value to convert.
   * @return          Converted value.
   * @throws IllegalArgumentException  If fromUnit or toUnit is null.
   */
  public static double convert(Unit fromUnit, Unit toUnit, double value)
  {
    if (fromUnit == null)
      throw new IllegalArgumentException("fromUnit cannot be null");

    if (toUnit == null)
      throw new IllegalArgumentException("toUnit cannot be null");

    double baseValue = fromUnit.toBase(value);
    return toUnit.fromBase(baseValue);
  }

  /**
   * Convert the specified value between the two given units.
   * <p>
   * Note that it is the client responsibility to check if it makes sense to
   * convert between the given units. This method simply converts the value
   * to base of the from unit, and convert this result from base of the to unit,
   * without considering the compatibility between the two.
   *
   * @param fromUnitSymbol  Unit symbol of current unit of value. Non-null.
   * @param toUnitSymbol    Unit symbol of unit to convert to. Non-null.
   * @param value           Value to convert.
   * @return                Converted value, or the input value it unit symbols
   *                        are unknown.
   */
  public double convert(String fromUnitSymbol, String toUnitSymbol, double value)
  {
    if (fromUnitSymbol == null)
      throw new IllegalArgumentException("fromUnitSymbol cannot be null");

    if (toUnitSymbol == null)
      throw new IllegalArgumentException("toUnitSymbol cannot be null");

    Unit fromUnit = findUnit(fromUnitSymbol);
    Unit toUnit = findUnit(toUnitSymbol);

    return fromUnit != null && toUnit != null ? convert(fromUnit, toUnit, value) : value;
  }

  /**
   * Return the display symbol for the given unit.
   *
   * @param unit  Unit to get display symbol of. As a convenience for
   *              the client, null is allowed, in case an empty string is
   *              returned.
   * @return      Display symbol of specified unit.
   *              Empty string if unitless. Never null.
   */
  public String getDisplaySymbol(Unit unit)
  {
    // Convenience to avoid excessive tests in client code
    if (unit == null)
      return "";

    String unitSymbol = unit.getSymbol();

    //
    // See if there is an explicit mapping
    //
    if (displaySymbols_.containsKey(unitSymbol))
      return displaySymbols_.getProperty(unitSymbol);

    if (unitSymbol.toLowerCase(Locale.US).equals("unitless"))
      return "";

    // Degree symbol in Celsius
    unitSymbol = unitSymbol.replaceAll("(?i)degc", "\u00b0C"); // case insensitive

    // Degree symbol in Fahrenheit
    unitSymbol = unitSymbol.replaceAll("(?i)degf", "\u00b0F"); // case insensitive

    // Degree symbol in R
    unitSymbol = unitSymbol.replaceAll("(?i)degr", "\u00b0R"); // case insensitive

    // Degree symbol in angles
    unitSymbol = unitSymbol.replaceAll("dega", "\u00b0");

    // Greek ohm symbol for ohm
    unitSymbol = unitSymbol.replaceAll("ohm", "\u2126");

    int n = unitSymbol.length();

    StringBuilder s = new StringBuilder();

    char prev = ' ';
    char c;
    char next = n > 0 ? unitSymbol.charAt(0) : ' ';

    for (int i = 0; i < n; i++) {
      c = next;
      next = i < n - 1 ? unitSymbol.charAt(i + 1) : ' ';

      //
      // Powers of 2
      //
      if (c == '2') {
        s.append(Character.isDigit(next) || Character.isDigit(prev) ? c : '\u00b2');
      }

      //
      // Powers of 3
      //
      else if (c == '3') {
        s.append(Character.isDigit(next) || Character.isDigit(prev) ? c : '\u00b3');
      }

      //
      // u = micro
      //
      else if (c == 'u') {
        s.append(i == 0 || prev == '/' ? '\u00b5' : c);
      }

      //
      // . = multiplication
      //
      else if (c == '.') {
        s.append(Character.isDigit(prev) && Character.isDigit(next) ? c : '\u00b7');
      }

      //
      // Anything else
      //
      else {
        s.append(c);
      }

      prev = c;
    }

    return s.toString();
  }

  /**
   * Return the display symbol for the given unit.
   *
   * @param unitSymbol  Unit symbol of the unit to get display symbol of. Non-null.
   * @return            Display symbol of specified unit. Null if unitless.
   */
  public String getDisplaySymbol(String unitSymbol)
  {
    Unit unit = findUnit(unitSymbol);
    return unit != null ? getDisplaySymbol(unit) : unitSymbol;
  }

  /**
   * Find quantity of the specified name, or create it if it is not found.
   *
   * @param quantityName  Name of quantity to find or create. Non-null.
   * @param description   Quantity description. May be null if
   *                      no description has been provided.
   * @return              Requested quantity. Never null.
   */
  private Quantity findOrCreateQuantity(String quantityName, String description)
  {
    assert quantityName != null : "quantityName cannot be null";

    Quantity quantity = findQuantity(quantityName);
    if (quantity == null) {
      quantity = new Quantity(quantityName, description);
      quantities_.add(quantity);
    }

    return quantity;
  }

  /**
   * Load all unit aliases from local properties file.
   */
  private void loadUnitAliases()
  {
    InputStream stream = null;

    try {
      stream = UnitManager.class.getResourceAsStream(UNIT_ALIASES_FILE);
      unitAliases_.load(stream);
    }
    catch (IOException exception) {
      // Ignore. If the file is not available we can run without
    }
    finally {
      if (stream != null) {
        try {
          stream.close();
        }
        catch (IOException exception) {
          // Ignore.
        }
      }
    }
  }

  /**
   * Load all display symbols from local properties file.
   */
  private void loadDisplaySymbols()
  {
    InputStream stream = null;

    try {
      stream = UnitManager.class.getResourceAsStream(DISPLAY_SYMBOLS_FILE);
      displaySymbols_.load(stream);
    }
    catch (IOException exception) {
      // Ignore. If the file is not available we can run without
    }
    finally {
      if (stream != null) {
        try {
          stream.close();
        }
        catch (IOException exception) {
          // Ignore.
        }
      }
    }
  }

  /**
   * Load all quantity and unit information from local XML file.
   */
  private void loadEnergisticsQuantities()
  {
    String packageName = getClass().getPackage().getName();
    String packageLocation = packageName.replace('.', '/');
    String filePath = "/" + packageLocation + "/" + UNITS_FILE;

    InputStream stream = UnitManager.class.getResourceAsStream(filePath);

    try {
      Document document = XmlUtil.newDocument(stream);

      Element rootElement = document.getDocumentElement();

      //
      // Version
      //
      Element unitDefinitionsElement = XmlUtil.getChild(rootElement, "UnitsDefinition");
      List<Element> unitOfMeasureElements = XmlUtil.findChildren(unitDefinitionsElement, "UnitOfMeasure");

      for (Element unitOfMeasureElement : unitOfMeasureElements) {
        List<Quantity> quantitiesForUnit = new ArrayList<>();

        //
        // Extract the BaseUnit element with its Description member
        //
        Element deprecatedElement = XmlUtil.getChild(unitOfMeasureElement, "Deprecated");
        Element baseUnitElement = XmlUtil.getChild(unitOfMeasureElement, "BaseUnit");
        boolean isBaseUnit = baseUnitElement != null && deprecatedElement == null;
        String quantityDescription = baseUnitElement != null ? XmlUtil.getChildValue(unitOfMeasureElement, "Description", null) : null;

        //
        // Identify all the quantities this unit appears in
        //
        List<Element> quantityTypeElements = XmlUtil.findChildren(unitOfMeasureElement, "QuantityType");
        for (Element quantityTypeElement : quantityTypeElements) {
          String quantityName = quantityTypeElement.getTextContent();
          Quantity quantity = findOrCreateQuantity(quantityName, quantityDescription);

          quantitiesForUnit.add(quantity);
        }

        String unitName = XmlUtil.getChildValue(unitOfMeasureElement, "Name", null);
        String unitSymbol = XmlUtil.getChildValue(unitOfMeasureElement, "CatalogSymbol", null);
        String id = XmlUtil.getAttribute(unitOfMeasureElement, "annotation", null);

        Element conversionElement = XmlUtil.getChild(unitOfMeasureElement, "ConversionToBaseUnit");

        double a = 1.0;
        double b = 0.0;
        double c = 0.0;
        double d = 1.0;

        if (conversionElement != null) {
          String factorText = XmlUtil.getChildValue(conversionElement, "Factor", null);

          Element fractionElement = XmlUtil.getChild(conversionElement, "Fraction");
          Element formulaElement = XmlUtil.getChild(conversionElement, "Formula");

          if (factorText != null) {
            try {
              a = Double.parseDouble(factorText);
            }
            catch (NumberFormatException exception) {
              assert false : "Invalid numeric value: " + factorText;
            }
          }
          else if (fractionElement != null) {
            String numeratorText = XmlUtil.getChildValue(fractionElement, "Numerator", null);
            String denominatorText = XmlUtil.getChildValue(fractionElement, "Denominator", null);

            try {
              double numerator = Double.parseDouble(numeratorText);
              double denominator = Double.parseDouble(denominatorText);

              a = numerator / denominator;
            }
            catch (NumberFormatException exception) {
              assert false : "Invalid numeric value: " + numeratorText + "/" + denominatorText;
            }
          }
          else if (formulaElement != null) {
            String aText = XmlUtil.getChildValue(formulaElement, "A", null);
            String bText = XmlUtil.getChildValue(formulaElement, "B", null);
            String cText = XmlUtil.getChildValue(formulaElement, "C", null);
            String dText = XmlUtil.getChildValue(formulaElement, "D", null);

            try {
              // NOTE: Uom has different defintion of a, b, c, d than Energistics
              //       so the switch of order is intentional
              b = Double.parseDouble(aText);
              a = Double.parseDouble(bText);
              d = Double.parseDouble(cText);
              c = Double.parseDouble(dText);
            }
            catch (NumberFormatException exception) {
              assert false : "Invalid numeric value: " + aText + "," + bText + "," + cText + "," + dText;
            }
          }
        }

        Unit unit = new Unit(unitName, unitSymbol, a, b, c, d);

        for (Quantity quantity : quantitiesForUnit) {
          quantity.addUnit(unit, isBaseUnit);
        }
      }
    }
    catch (SAXException | IOException exception) {
      assert false : "Parse error: " + filePath;
    }
  }

  /** {@inheritDoc} */
  @Override
  public String toString()
  {
    StringBuilder s = new StringBuilder();

    s.append("Quantities....: " + quantities_.size() + "\n");
    int nUnits = 0;
    for (Quantity quantity : quantities_)
      nUnits += quantity.getUnits().size();
    s.append("Units.........: " + nUnits + "\n");
    s.append("Unit aliases..: " + unitAliases_.size());

    return s.toString();
  }
}
