# UoM - Units of measurement library

UoM is Java API convenient front-end to the Energistics
[Unit of Measure Standard](https://www.energistics.org/energistics-unit-of-measure-standard/).

![UoM Library](https://petroware.no/images/UomBox.250.png)

UoM web page: https://petroware.no/uom.html


## Setup

Capture the UoM code to local disk by:

```
$ git clone https://github.com/Petroware/Uom.git
```


## Dependencies

UoM has no external dependenies. The Energistics unit database
(`./src/no/petroware.uom/witsmlUnitDict.xml`) is embedded in the library.


## Building UoM

UoM can be built from its root folder by

```
$ make clean
$ make
$ make jar
```

The UoM delivery will be the `./lib/Uom.jar` file.

Building with make requires the make module of the tools repository.


## Javadoc

Public Javadoc: https://petroware.no/uom/javadoc/index.html

Javadoc can be created locally by:

```
$ make javadoc
```

Entry point will be `./docs/index.html`.

Note the `./overview.html` page that becomes part of the Javadoc.

Note also that there is some Javadoc configuration in `./Makefile`. The Javadoc is not
automatically date stamped. The Javadoc date (current month) is specified in the Makefile.



## Programming examples

The easiest way to get started with UoM is to explore the predefined
Energistics quantities and units:

```java
import no.petroware.uom.Quantity;
import no.petroware.uom.Unit;
import no.petroware.uom.UnitManager;

:

//
// Get the unit manager singleton
//
UnitManager unitManager = UnitManager.getInstance();

//
// Get all pre-defined quantities and their units
//
List<Quantity> quantities = uniteManager.getQuantities();
for (Quantity : quantities) {
  System.out.println("Quantity: " + quantity.getName() + " - " + quantity.getDescription();
  for (Unit unit : quantity.getUnits())
    System.out.println("  Unit: " + unit.getSymbol() + " (" + unit.getName() + ")";
}
```

### Unit conversion

Basic unit conversion is done through ``UnitManager``
using instances of ``Unit`` or the unit symbols directly:

```java
//
// Convert between known units, using unit symbols directly
//
double milesPerHour = 55.0;
double kilometersPerHour = unitManager.convert("mi/h", "km/h", milesPerHour);

//
// Conversion using Unit instances
//
Unit feet = unitManager.findUnit("ft");

double lengthFt = 8981.0; // Length of the Golden Gate bridge in feet

Quantity quantity = unitManager.findQuantity("length");
for (Unit unit : quantity.getUnits()) {
  double length = unitManager.convert(feet, unit, lengthFt);
  System.out.println("Golden Gate is " + length + " " + unit.getSymbol());
}
```

Making a user interface units aware includes associating
GUI components with quantities and then provide unit conversions,
either per element or as overall preference settings.

It is essential that the application knows the initial unit of measure
of the values involved. A common advice to reduce complexity and risk of errors
is to keep the entire data model in _base_ units (typically
![SI](https://en.wikipedia.org/wiki/International_System_of_Units")
or similar) and convert in GUI on users request. The associated units will then
be _implied_, effectively making the entire business logic _unitless_.
Conversions to and from base units can be done directly on the ``Unit`` instances:

```java
//
// Capture pressure display unit from GUI or prefernces
//
Unit diplsayUnit = ...;
String displaySymbol = unitManager.getDisplaySymbol(diplsayUnit.getSymbol());

//
// Populate GUI element
//
double pressure = ...; // From business model, SI implied
pressureText.setText(displayUnit.fromBase(pressure) + " [" + displaySymbol + "]");

:

//
// Capture user input
//
double value = pressureText.getValue(); // In user preferred unit
double pressure = displayUnit.toBase(value); // Converted to business model unit (SI)
```

It may make sense to provide unit conversion even if the quantity of a measure
is unknown. In these cases it is possible to obtain the quantity, but it might
be more convenient to get all convertible units directly:

```java
//
// Given a unit, find the associated quantity
//
String unitSymbol = "degC"; // Degrees Celsius
Quantity quanitity = unitManager.findQuantity(unitSymbol);
List<Unit> units = quantity.getUnits(); // All temperature units

:

//
// Given a unit, find all convertible units
//
String unitSymbol = "degC"; // Degrees Celsius
List<Unit> units = unitManager.findConvertibleUnits(unitSymbol);
```

### Unit aliases

There is no universal accepted standard or convention for unit symbols, and
to make the module more robust when dealing with units from various sources
it is possible to add unit <em>aliases</em>. UoM uses the unit symbols defined
by Energistics, but have added many aliases for common notations.
In addition, client applications can easily supply their own:

```java
unitManager.addUnitAlias("m/s^2", "m/s2");
unitManager.addUnitAlias("inch", "in");
unitManager.addUnitAlias("api", "gAPI");
unitManager.addUnitAlias("deg", "dega");
```

The typical approach would be to read these from a properties file during startup.

### Display symbols

Unit symbols should be regarded as _IDs_, and clients
should never expose these in a user interface. A GUI friendly _display symbol_ may be
obtained through the ``UnitManager.getDisplaySymbol()`` method.

The table below indicates
the connection between _unit name_, _unit symbol_ and _display symbol_:

| Unit name             | Unit symbol | Display symbol   |
|-----------------------|-------------|------------------|
| microseconds per foot | us/ft       | &#181;s/ft       |
| ohm meter             | ohmm        | &#8486;&middot;m |
| cubic centimeters     | cm3         | cm<sup>3</sup>   |
| degrees Celcius       | degC        | &deg;C           |
| meter/second squared  | m/s2        | m/s<sup>2</sup>  |
| etc.                  |             |                  |

As for unit aliases, it is possible for clients to supply their own
specific display symbols through the ``UnitManager.setDisplaySymbol()`` method.

### Extensibility

If the predefined set of quantities and units is not sufficient, a client may
easily supply their own:

```java
//
// Define a "computer storage" quantity with associated units
//
Quantity q = new Quantity("computer storage");
q.addUnit(new Unit("byte", "byte", 1.0, 0.0, 0.0, 1.0), true);
q.addUnit(new Unit("kilo byte", "kB", 1.0e3, 0.0, 0.0, 1.0), false);
q.addUnit(new Unit("mega byte", "MB", 1.0e6, 0.0, 0.0, 1.0), false);
q.addUnit(new Unit("giga byte", "GB", 1.0e9, 0.0, 0.0, 1.0), false);
:
unitManager.addQuantity(q);

//
// Test the new units
//
long nBytes = 1230000L;
double nMegaBytes = unitManager.convert("byte", "MB", nBytes); // 1.23
```

It is also possible to add units to existing quantities:

```java
//
// Add "light year" unit to the existing "length" quantity
//
Quantity q = unitManager.findQuantity("length");
q.addUnit(new Unit("light year", "ly", 9.4607e15, 0.0, 0.0, 1.0), false);
```
