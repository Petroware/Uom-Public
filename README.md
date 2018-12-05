# UoM - Units of measurement library

When dealing with scientific data it is essential to know the units of
measurement in order to _understand_ and present the information correctly.
Likewise, in order to do _computations_ with scientific data it is essential
that software is able to convert data into a common unit framework.

<img hspace="100" src="https://petroware.no/images/UomBox.250.png">

The Petroware UoM library is a convenient, extensible front-end to the
[Energistics Unit of Measure](https://www.energistics.org/energistics-unit-of-measure-standard/)
database.
It contains definitions of more than
[2500 units](http://w3.energistics.org/uom/poscUnits22.xml)
from more than 250 different quantities.
The API is simple, well documented and easy to use, and the library is trivial
to embed in any scientific software system.

UoM is available for Java (`Uom.jar`) and .Net (`Uom.dll`).
The library is lightweight (< 0.1MB) and self-contained; It embeds the complete
Energistics unit database and has no external dependencies.

UoM web page: https://petroware.no/uom.html


## Setup

Capture the UoM code to local disk by:

```
$ git clone https://github.com/Petroware/Uom.git
```

The Java code is in the `./src` tree, while the C# code is in the `./Petroware` tree.
The compiled libraries are in the `./lib` folder.


## Dependencies

UoM has no external dependenies. The Energistics unit database
(`./src/no/petroware/uom/witsmlUnitDict.xml` and `./Petroware/LogIo/Uom/witsmlUnitDict.xml`)
is embedded in the library.


## API Documentation

Java: https://petroware.no/uom/javadoc/index.html

.Net: https://petroware.no/uom/doxygen/index.html


## Programming examples

The examples below are for the Java version of UoM. The C# API is similar.

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
[SI](https://en.wikipedia.org/wiki/International_System_of_Units)
or similar) and convert in GUI on users request. The associated units will then
be _implied_, effectively making the entire business logic _unitless_.
Conversions to and from base units can be done directly on the ``Unit`` instances:

```java
//
// Capture pressure display unit from GUI or preferences
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

## About Petroware

Petroware AS is a software company within the data management, data analytics,
petrophysics, geology and reservoir engineering domains.

Petroware creates highly advanced software components and end-user products that
acts as a research platform within software architecture and scalability, system design,
parallelism and multi-threading, user experience (UX) and usability analysis as well
as development methodologies and techniques.

**Petroware AS**<br>
Stavanger - Norway<br>
[https://petroware.no](https://petroware.no)<br>
info@petroware.no
