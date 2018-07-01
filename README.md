# UoM - Units of measurement library #

UoM is Java API convenient front-end to the Energistics
[Unit of Measure Standard](http://www.energistics.org/asset-data-management/unit-of-measure-standard).

![UoM Library](https://petroware.no/images/UomBox.250.png)

UoM webpage: https://petroware.no/uom.html


### Setup ###

Capture the UoM code to local disk by:

```
$ git clone https://github.org/Petroware/Uom
```


### Dependencies ###

UoM has no external dependenies. The Energistics unit database
(`./src/no/petroware.uom/witsmlUnitDict.xml`) is embedded in the library.


### Building UoM ###

UoM can be built from its root folder by

```
$ make clean
$ make
$ make jar
```

The UoM delivery will be the `./lib/Uom.jar` file.

Building with make requires the make module of the tools reprository.


### Creating Javadoc ###

Javadoc can be created by:

```
$ make javadoc
```

Entry point will be `./docs/index.html`.

Note the `./overview.html` page that becomes part of the Javadoc.

Note also that there is some Javadoc configuration in `./Makefile`. The Javadoc is not
automatically date stamped. The Javadoc date (current month) is specified in the Makefile.
