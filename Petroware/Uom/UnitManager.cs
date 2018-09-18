using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;

namespace Petroware.Uom
{
  /// <summary>
  ///   Units of measurement manager.
  ///
  ///   Singleton instance and main access point for quantities, units and
  ///   unit conversions.
  ///
  ///   This class is thread-safe.
  ///
  ///   \author <a href="mailto:info@petroware.no">Petroware AS</a>
  /// </summary>
  public sealed class UnitManager
  {
    /// <summary>
    ///   The XML file with the Energistics unit database.
    /// </summary>
    private const string UNITS_FILE = "Petroware.Uom.witsmlUnitDict-2.2.xml";

    /// <summary>
    ///   Property file holding unit aliases.
    /// </summary>
    private const string UNIT_ALIASES_FILE = "Petroware.Uom.unit_aliases.txt";

    /// <summary>
    ///   Property file holding display symbols.
    /// </summary>
    private const string DISPLAY_SYMBOLS_FILE = "Petroware.Uom.display_symbols.txt";

    /// <summary>
    ///   The sole instance of this class.
    /// </summary>
    private static UnitManager instance_ = new UnitManager();

    /// <summary>
    ///   Mapping between unit symbol alias and their equivalent "official" unit symbol.
    /// </summary>
    private readonly Dictionary<string,string> unitAliases_ = new Dictionary<string,string>();

    /// <summary>
    ///   Mapping between unit symbol and its equivalent display symbol.
    /// </summary>
    private readonly Dictionary<string,string> displaySymbols_ = new Dictionary<string,string>();

    /// <summary>
    ///   Quantities known by this manager.
    /// </summary>
    private readonly List<Quantity> quantities_ = new List<Quantity>(); // TODO: CopyOnWriteArrayList<>();

    /// <summary>
    ///   Return the sole instance of this class.
    /// </summary>
    ///
    /// <returns>
    ///   The sole instance of this class. Never null.
    /// </returns>
    public static UnitManager GetInstance()
    {
      return instance_;
    }

    /// <summary>
    ///   Create a unit manager instance.
    /// </summary>
    private UnitManager()
    {
      LoadEnergisticsQuantities();
      LoadUnitAliases();
      LoadDisplaySymbols();
    }

    /// <summary>
    ///   Add specified alias to be associated with the given official
    ///   unit symbol.
    ///
    ///   The alias will be used when identifying Unit instances
    ///   from unit symbols and affects all methods of this class taking
    ///   unit symbol as argument.
    ///
    ///   Multiple aliases can be added for each unit symbol.
    /// </summary>
    ///
    /// <param name="unitSymbolAlias">
    ///   Alias to add. Non-null.
    /// </param>
    /// <param name="unitSymbol">
    ///   Unit symbol to associated alias with. Non-null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   If unitSymbolAlias or unitSymbol is null.
    /// </exception>
    public void AddUnitAlias(string unitSymbolAlias, string unitSymbol)
    {
      if (unitSymbolAlias == null)
        throw new ArgumentNullException("unitSymbolAlias");

      if (unitSymbol == null)
        throw new ArgumentNullException("unitSymbol");

      unitAliases_[unitSymbolAlias] = unitSymbol;
    }

    /// <summary>
    ///   Set the specified display symbol for the given unit symbol.
    /// </summary>
    ///
    /// <param name="unitSymbol">
    ///   Official unit symbol. Non-null.
    /// </param>
    /// <param name="displaySymbol">
    ///   Associated display symbol. Non-null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   If unitSymbol or displaySymbol is null.
    /// </exception>
    public void SetDisplaySymbol(string unitSymbol, string displaySymbol)
    {
      if (unitSymbol == null)
        throw new ArgumentNullException("unitSymbol");

      if (displaySymbol == null)
        throw new ArgumentNullException("displaySymbol");

      displaySymbols_[unitSymbol] = displaySymbol;
    }

    /// <summary>
    ///   Set the specified display symbol for the given unit.
    /// </summary>
    ///
    /// <param name="unit">
    ///   Unit to set display symbol of. Non-null.
    /// </param>
    /// <param name="displaySymbol">
    ///   Associated display symbol. Non-null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   If unitSymbol or displaySymbol is null.
    /// </exception>
    public void SetDisplaySymbol(Unit unit, string displaySymbol)
    {
      if (unit == null)
        throw new ArgumentNullException("unit");

      if (displaySymbol == null)
        throw new ArgumentNullException("displaySymbol");

      SetDisplaySymbol(unit.GetSymbol(), displaySymbol);
    }

    /// <summary>
    ///   Return all quantities known by this unit manager.
    /// </summary>
    ///
    /// <returns>
    ///   All quantities. Never null.
    /// </returns>
    public IList<Quantity> GetQuantities()
    {
      return quantities_.AsReadOnly();
    }

    /// <summary>
    ///   Add the specified quantity to this unit manager.
    /// </summary>
    ///
    /// <param name="quantity">
    ///   Quantity to add. Non-null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   If quantity is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   If quantity is already contained in this manager.
    /// </exception>
    public void AddQuantity(Quantity quantity)
    {
      if (quantity == null)
        throw new ArgumentNullException("quantity");

      if (FindQuantity(quantity.GetName()) != null)
        throw new ArgumentException("Quantity is already present: " + quantity.GetName());

      quantities_.Add(quantity);
    }

    /// <summary>
    ///   Find quantity of the given name.
    /// </summary>
    ///
    /// <param name="quantityName">
    ///   Name of quantity to find. Non-null.
    /// </param>
    /// <returns>
    ///   Requested quantity or null if not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If quantityName is null.
    /// </exception>
    public Quantity FindQuantity(string quantityName)
    {
      if (quantityName == null)
        throw new ArgumentNullException("quantityName");

      foreach (Quantity quantity in quantities_) {
        if (quantity.GetName().Equals(quantityName))
          return quantity;
      }

      // Not found
      return null;
    }

    /// <summary>
    ///   Find corresponding unit instance for the given unit symbol.
    ///
    ///   The alias mapping is considered, and units are searched both case
    ///   sensitive and case insensitive.
    /// </summary>
    ///
    /// <param name="unitSymbol">
    ///   Unit symbol to find unit for. May be null for unitless.
    /// </param>
    /// <returns>
    ///   Associated unit, or null if not found.
    /// </returns>
    public Unit FindUnit(string unitSymbol)
    {
      if (unitSymbol == null || unitSymbol.Trim().Length == 0)
        unitSymbol = "unitless";

      string lowerCase = unitSymbol.ToLower().Trim();

      // Check if there is an explicit mapping
      if (unitAliases_.ContainsKey(lowerCase))
        unitSymbol = unitAliases_[lowerCase];

      // Loop over all units to see if there is a matching one
      // with same case
      foreach (Quantity quantity in quantities_) {
        foreach (Unit unit in quantity.GetUnits()) {
          string symbol = unit.GetSymbol();
          if (unitSymbol.Equals(symbol))
            return unit;
        }
      }

      // Do the same, but case insensitive this time
      foreach (Quantity quantity in quantities_) {
        foreach (Unit unit in quantity.GetUnits()) {
          string symbol = unit.GetSymbol();
          string symbolLowerCase = symbol.ToLower();
          if (lowerCase.Equals(symbolLowerCase))
            return unit;
        }
      }

      // Not found
      return null;
    }

    /// <summary>
    ///   Return all units that are convertible with the specified unit.
    /// </summary>
    ///
    /// <param name="unit">
    ///   Unit to consider. Non-null.
    /// </param>
    /// <returns>
    ///   All convertible units. Never null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If unit is null.
    /// </exception>
    public IList<Unit> FindConvertibleUnits(Unit unit)
    {
      if (unit == null)
        throw new ArgumentNullException("unit");

      HashSet<Unit> units = new HashSet<Unit>();

      foreach (Quantity quantity in quantities_) {
        if (quantity.GetUnits().Contains(unit))
          units.UnionWith(quantity.GetUnits());
      }

      units.Remove(unit);

      return new List<Unit>(units);
    }

    /// <summary>
    ///   Return all units that are convertible with the unit of the
    ///   specified symbol.
    /// </summary>
    ///
    /// <param name="unitSymbol">
    ///   Symbol of unit to consider. Null if unitless.
    /// </param>
    /// <returns>
    ///   All convertible units. Never null.
    /// </returns>
    public IList<Unit> FindConvertibleUnits(string unitSymbol)
    {
      Unit unit = FindUnit(unitSymbol);
      return unit != null ? FindConvertibleUnits(unit) : new List<Unit>();
    }

    /// <summary>
    ///   Return all quantities that includes the specified unit.
    /// </summary>
    ///
    /// <param name="unit">
    ///   Unit to consider. Non-null.
    /// </param>
    /// <returns>
    ///   Requested quantities. Never null.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If unit is null.
    /// </exception>
    public IList<Quantity> FindQuantities(Unit unit)
    {
      if (unit == null)
        throw new ArgumentNullException("unit");

      List<Quantity> quantities = new List<Quantity>();

      foreach (Quantity quantity in quantities_) {
        if (quantity.GetUnits().Contains(unit))
          quantities.Add(quantity);
      }

      return quantities;
    }

    /// <summary>
    ///   Return all quantities that includes the unit of the specified symbol.
    /// </summary>
    ///
    /// <param name="unitSymbol">
    ///   Unit symbol of unit to consider. Null if unitless.
    /// </param>
    /// <returns>
    ///   Requested quantities. Never null.
    /// </returns>
    public IList<Quantity> FindQuantities(string unitSymbol)
    {
      Unit unit = FindUnit(unitSymbol);
      return unit != null ? FindQuantities(unit) : new List<Quantity>();
    }

    /// <summary>
    ///   Find quantity of the specified unit.
    ///
    ///   Note that a unit may be contained in multiple quantities.
    ///   This method is convenient if the client knows that the unit
    ///   exists in one quantity only. If it exists in more than one
    ///   quantity, the first one encountered is returned.
    /// </summary>
    ///
    /// <param name="unit">
    ///   Unit to consider. Non-null.
    /// </param>
    /// <returns>
    ///   Requested quantity or null if none found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If unit is null.
    /// </exception>
    public Quantity FindQuantity(Unit unit)
    {
      if (unit == null)
        throw new ArgumentNullException("unit");

      IList<Quantity> quantities = FindQuantities(unit);

      if (quantities.Count == 0)
        return null;

      else if (quantities.Count == 1)
        return quantities[0];

      else {
        // Unit clash: "Siemens (S) - seconds (s)"
        if (quantities.Contains(FindQuantity("time")))
          return FindQuantity("time");

        // TODO: Others

        return quantities[0];
      }
    }

    /// <summary>
    ///   Check if it is possible to convert between the two specified units.
    /// </summary>
    ///
    /// <param name="unit1">
    ///   First unit to consider. Non-null.
    /// </param>
    /// <param name="unit2">
    ///   Second unit to consider. Non-null.
    /// </param>
    /// <returns>
    ///   True if it is possible to convert between the two,
    ///   false otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If unit1 or unit2 is null.
    /// </exception>
    public bool CanConvert(Unit unit1, Unit unit2)
    {
      if (unit1 == null)
        throw new ArgumentNullException("unit1");

      if (unit2 == null)
        throw new ArgumentNullException("unit2");

      IList<Quantity> quantities1 = FindQuantities(unit1);
      IList<Quantity> quantities2 = FindQuantities(unit2);

      foreach (Quantity quantity in quantities1)
        if (quantities2.Contains(quantity))
          return true;

      return false;
    }

    /// <summary>
    ///   Check if it is possible to convert between the two specified units.
    /// </summary>
    ///
    /// <param name="unitSymbol1">
    ///   Unit symbol of first unit to consider. Null if unitless.
    /// </param>
    /// <param name="unitSymbol2">
    ///   Unit symbol of second unit to consider. Null if unitless.
    /// </param>
    /// <returns>
    ///   True if it is possible to convert between the two,
    ///   false otherwise.
    /// </returns>
    public bool CanConvert(string unitSymbol1, string unitSymbol2)
    {
      Unit unit1 = FindUnit(unitSymbol1);
      Unit unit2 = FindUnit(unitSymbol2);

      return unit1 == null || unit2 == null ? CanConvert(unit1, unit2) : false;
    }

    /// <summary>
    ///   Convert the specified value between the two given units.
    ///
    ///   Note that it is the client responsibility to check if it makes sense to
    ///   convert between the given units. This method simply converts the value
    ///   to base of the from unit, and convert this result from base of the to unit,
    ///   without considering the compatibility between the two.
    /// </summary>
    ///
    /// <param name="fromUnit">
    ///   Current unit of value. Non-null.
    /// </param>
    /// <param name="toUnit">
    ///   Unit to convert to. Non-null.
    /// </param>
    /// <param name="value">
    ///   Value to convert.
    /// </param>
    /// <returns>
    ///   Converted value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If fromUnit or toUnit is null.
    /// </exception>
    public static double Convert(Unit fromUnit, Unit toUnit, double value)
    {
      if (fromUnit == null)
        throw new ArgumentNullException("fromUnit");

      if (toUnit == null)
        throw new ArgumentNullException("toUnit");

      double baseValue = fromUnit.ToBase(value);
      return toUnit.FromBase(baseValue);
    }

    /// <summary>
    ///  Convert the specified value between the two given units.
    ///
    ///  Note that it is the client responsibility to check if it makes sense to
    ///  convert between the given units. This method simply converts the value
    ///  to base of the from unit, and convert this result from base of the to unit,
    ///  without considering the compatibility between the two.
    /// </summary>
    ///
    /// <param name="fromUnitSymbol">
    ///   Unit symbol of current unit of value. Non-null.
    /// </param>
    /// <param name="toUnitSymbol">
    ///   Unit symbol of unit to convert to. Non-null.
    /// </param>
    /// <param name="value">
    ///   Value to convert.
    /// </param>
    /// <returns>
    ///   Converted value, or the input value it unit symbols
    ///   are unknown.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   If fromUnitSymbol or toUnitSymbol is null.
    /// </exception>
    public double Convert(string fromUnitSymbol, string toUnitSymbol, double value)
    {
      if (fromUnitSymbol == null)
        throw new ArgumentNullException("fromUnitSymbol");

      if (toUnitSymbol == null)
        throw new ArgumentNullException("toUnitSymbol");

      Unit fromUnit = FindUnit(fromUnitSymbol);
      Unit toUnit = FindUnit(toUnitSymbol);

      return fromUnit != null && toUnit != null ? Convert(fromUnit, toUnit, value) : value;
    }

    /// <summary>
    ///   Return the display symbol for the given unit.
    /// </summary>
    ///
    /// <param name="unit">
    ///   Unit to get display symbol of. As a convenience for the client,
    ///   null is allowed, in case an empty string is returned.
    /// </param>
    /// <returns>
    ///   Display symbol of specified unit.
    ///   Empty string if unitless. Never null.
    /// </returns>
    public string GetDisplaySymbol(Unit unit)
    {
      // Convenience to avoid excessive tests in client code
      if (unit == null)
        return "";

      string unitSymbol = unit.GetSymbol();

      //
      // See if there is an explicit mapping
      //
      if (displaySymbols_.ContainsKey(unitSymbol))
        return displaySymbols_[unitSymbol];

      if (unitSymbol.ToLower().Equals("unitless"))
        return "";

      // Degree symbol in Celsius
      unitSymbol = unitSymbol.Replace("(?i)degc", "\u00b0C"); // case insensitive

      // Degree symbol in Fahrenheit
      unitSymbol = unitSymbol.Replace("(?i)degf", "\u00b0F"); // case insensitive

      // Degree symbol in R
      unitSymbol = unitSymbol.Replace("(?i)degr", "\u00b0R"); // case insensitive

      // Degree symbol in angles
      unitSymbol = unitSymbol.Replace("dega", "\u00b0");

      // Greek ohm symbol for ohm
      unitSymbol = unitSymbol.Replace("ohm", "\u2126");

      int n = unitSymbol.Length;

      StringBuilder s = new StringBuilder();

      char prev = ' ';
      char c;
      char next = n > 0 ? unitSymbol[0] : ' ';

      for (int i = 0; i < n; i++) {
        c = next;
        next = i < n - 1 ? unitSymbol[i + 1] : ' ';

        //
        // Powers of 2
        //
        if (c == '2') {
          s.Append(Char.IsDigit(next) || Char.IsDigit(prev) ? c : '\u00b2');
        }

        //
        // Powers of 3
        //
        else if (c == '3') {
          s.Append(Char.IsDigit(next) || Char.IsDigit(prev) ? c : '\u00b3');
        }

        //
        // u = micro
        //
        else if (c == 'u') {
          s.Append(i == 0 || prev == '/' ? '\u00b5' : c);
        }

        //
        // . = multiplication
        //
        else if (c == '.') {
          s.Append(Char.IsDigit(prev) && Char.IsDigit(next) ? c : '\u00b7');
        }

        //
        // Anything else
        //
        else {
          s.Append(c);
        }

        prev = c;
      }

      return s.ToString();
    }

    /// <summary>
    ///   Return the display symbol for the given unit.
    /// </summary>
    ///
    /// <param name="unitSymbol">
    ///   Unit symbol of the unit to get display symbol of. Non-null.
    /// </param>
    /// <returns>
    ///   Display symbol of specified unit. Null if unitless.
    /// </returns>
    public string GetDisplaySymbol(string unitSymbol)
    {
      Unit unit = FindUnit(unitSymbol);
      return unit != null ? GetDisplaySymbol(unit) : unitSymbol;
    }

    /// <summary>
    ///   Find quantity of the specified name, or create it if it is not found.
    /// </summary>
    ///
    /// <param name="quantityName">
    ///   Name of quantity to find or create. Non-null.
    /// </param>
    /// <param name="description">
    ///    Quantity description.
    ///    May be null if no description has been provided.
    /// </param>
    /// <returns>
    ///   Requested quantity. Never null.
    /// </returns>
    private Quantity FindOrCreateQuantity(string quantityName, string description)
    {
      Debug.Assert(quantityName != null, "quantityName cannot be null");

      Quantity quantity = FindQuantity(quantityName);
      if (quantity == null) {
        quantity = new Quantity(quantityName, description);
        quantities_.Add(quantity);
      }

      return quantity;
    }

    /// <summary>
    ///   Read content of the specified embedded resource file as text.
    /// </summary>
    ///
    /// <param name="fileName">
    ///   Name of file to read. Non-null
    /// </param>
    /// <returns>
    ///   The file content as a string.
    /// </returns>
    private string ReadResourceFile(string fileName)
    {
      Debug.Assert(fileName != null, "fileName cannot be null");

      Stream stream = null;

      Assembly assembly = GetType().Assembly;
      string assemblyName = assembly.GetName().Name;

      string name = assemblyName + "." + fileName;

      try {
        stream = assembly.GetManifestResourceStream(name);
        StreamReader reader = new StreamReader(stream);

        return reader.ReadToEnd();
      }
      catch (IOException) {
        // Ignore. If the file is not available we can run without
        return null;
      }
      finally {
        if (stream != null) {
          try {
            stream.Close();
          }
          catch (IOException) {
            // Ignore.
          }
        }
      }
    }

    /// <summary>
    ///   Load all unit aliases from local properties file.
    /// </summary>
    private void LoadUnitAliases()
    {
      string fileContent = ReadResourceFile(UNIT_ALIASES_FILE);

      string[] lines = fileContent.Split('\n');
      foreach (string line in lines) {
        string[] tokens = line.Split('=');
        if (tokens.Length == 2)
          unitAliases_[tokens[0]] = tokens[1];
      }
    }

    /// <summary>
    ///   Load all display symbols from local properties file.
    /// </summary>
    private void LoadDisplaySymbols()
    {
      string fileContent = ReadResourceFile(DISPLAY_SYMBOLS_FILE);

      string[] lines = fileContent.Split('\n');
      foreach (string line in lines) {
        string[] tokens = line.Split('=');
        if (tokens.Length == 2)
          displaySymbols_[tokens[0]] = tokens[1];
      }
    }

    /// <summary>
    ///   Convert the specified string to its equivalent double value.
    /// </summary>
    ///
    /// <param name="value">
    ///   String to convert.
    /// </param>
    /// <returns>
    ///   The requested value. NaN if the string cannot be converted to a double.
    /// </returns>
    private static double GetAsDouble(string value)
    {
      double v;
      bool isNumeric = Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
      return isNumeric ? v : Double.NaN;
    }

    /// <summary>
    ///    Load all quantity and unit information from local XML file.
    /// </summary>
    private void LoadEnergisticsQuantities()
    {
      string fileContent = ReadResourceFile(UNITS_FILE);

      XmlDocument document = new XmlDocument();
      document.LoadXml(fileContent);

      XmlElement rootElement = document.DocumentElement;

      //
      // UnitOfMeasure elements
      //
      XmlElement unitsDefinitionsElement = XmlUtil.GetChild(rootElement, "UnitsDefinition");
      List<XmlElement> unitOfMeasurementElements = XmlUtil.FindChildren(unitsDefinitionsElement, "UnitOfMeasure");

      foreach (XmlElement unitOfMeasureElement in unitOfMeasurementElements) {
        List<Quantity> quantitiesForUnit = new List<Quantity>();

        //
        // Extract the BaseUnit element with its Description member
        //
        XmlElement deprecatedElement = XmlUtil.GetChild(unitOfMeasureElement, "Deprecated");
        XmlElement baseUnitElement = XmlUtil.GetChild(unitOfMeasureElement, "BaseUnit");
        bool isBaseUnit = baseUnitElement != null && deprecatedElement == null;
        string quantityDescription = baseUnitElement != null ? XmlUtil.GetChildValue(baseUnitElement, "Description", null) : null;

        //
        // Identify all the quantities this unit appears in
        //
        List<XmlElement> quantityTypeElements = XmlUtil.FindChildren(unitOfMeasureElement, "QuantityType");
        foreach (XmlElement quantityTypeElement in quantityTypeElements) {
          string quantityName = quantityTypeElement.InnerText.Trim();
          Quantity quantity = FindOrCreateQuantity(quantityName, quantityDescription);

          quantitiesForUnit.Add(quantity);
        }

        string unitName = XmlUtil.GetChildValue(unitOfMeasureElement, "Name", null);
        string unitSymbol = XmlUtil.GetChildValue(unitOfMeasureElement, "CatalogSymbol", null);
        string id = XmlUtil.GetAttribute(unitOfMeasureElement, "annotation", (string) null);

        XmlElement conversionElement = XmlUtil.GetChild(unitOfMeasureElement, "ConversionToBaseUnit");

        double a = 1.0;
        double b = 0.0;
        double c = 0.0;
        double d = 1.0;

        if (conversionElement != null) {
          string factorText = XmlUtil.GetChildValue(conversionElement, "Factor", null);

          XmlElement fractionElement = XmlUtil.GetChild(conversionElement, "Fraction");
          XmlElement formulaElement = XmlUtil.GetChild(conversionElement, "Formula");

          if (factorText != null) {
            a = GetAsDouble(factorText);
          }
          else if (fractionElement != null) {
            string numeratorText = XmlUtil.GetChildValue(fractionElement, "Numerator", null);
            double numerator = GetAsDouble(numeratorText);

            string denominatorText = XmlUtil.GetChildValue(fractionElement, "Denominator", null);
            double denominator = GetAsDouble(denominatorText);

            a = numerator / denominator;
          }
          else if (formulaElement != null) {
            string aText = XmlUtil.GetChildValue(formulaElement, "A", null);
            string bText = XmlUtil.GetChildValue(formulaElement, "B", null);
            string cText = XmlUtil.GetChildValue(formulaElement, "C", null);
            string dText = XmlUtil.GetChildValue(formulaElement, "D", null);

            // NOTE: Uom has different defintion of a, b, c, d than Energistics
            //       so the switch of order is intentional
            b = GetAsDouble(aText);
            a = GetAsDouble(bText);
            d = GetAsDouble(cText);
            c = GetAsDouble(dText);
          }
        }

        Unit unit = new Unit(unitName, unitSymbol, a, b, c, d);

        foreach (Quantity quantity in quantitiesForUnit) {
          quantity.AddUnit(unit, isBaseUnit);
        }
      }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
      StringBuilder s = new StringBuilder();

      s.Append("Quantities....: " + quantities_.Count + "\n");
      int nUnits = 0;
      foreach (Quantity quantity in quantities_)
        nUnits += quantity.GetUnits().Count;
      s.Append("Units.........: " + nUnits + "\n");
      s.Append("Unit aliases..: " + unitAliases_.Count);

      return s.ToString();
    }
  }
}
