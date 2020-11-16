# Panasonic Camera Control

Controls the following cameras:

1. AW-HE20
1. AW-HE120
1. AW-HE60
1. AW-HE130
1. AW-HE70
1. AW-HE40
1. AW-SFU01
1. AK-UB300
1. AW-HR140
1. AW-UE150
1. AK-UB300
1. AW-UE150
1. AW-HE42

## Cloning Instructions

After forking this repository into your own GitHub space, you can create a new repository using this one as the template.  Then you must install the necessary dependencies as indicated below.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

[Control Documentation](https://eww.pass.panasonic.co.jp/pro-av/support/content/guide/DEF/HE50_120_IP/HDIntegratedCamera_InterfaceSpecifications-E.pdf)
