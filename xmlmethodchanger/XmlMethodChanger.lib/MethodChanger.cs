// /* MethodChanger.cs
//  *
//  * Copyright (C) 2016 Thermo Fisher Scientific
//  *
//  * This software may be modified and distributed under the terms
//  * of the MIT license.  See the LICENSE file for details.
//  */
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using Thermo.TNG.MethodXMLFactory;
using Thermo.TNG.MethodXMLInterface;

namespace XmlMethodChanger.lib
{
    public enum InstrumentFamily { OrbitrapFusion, TSQ, Merkur, Unknown };

    public static class MethodChanger
    {
        /// <summary>
        /// Helper function to get the MethodXMLContext
        /// </summary>
        /// <param name="model">The instrument model</param>
        /// <param name="version">The instrument version</param>
        /// <returns></returns>
        public static IMethodXMLContext CreateContext(string model = "", string version = "")
        {
            if (string.IsNullOrEmpty(model))
            {
                model = MethodXMLFactory.GetInstalledServerModel();
            }

            if (string.IsNullOrEmpty(version))
            {
                version = MethodXMLFactory.GetLatestInstalledVersion(model);
            }

            if (!MethodXMLFactory.GetInstalledInstrumentModels().Contains(model))
            {
                throw new ArgumentException("Cannot create method context for non-installed instrument model: " + model);
            }

            Console.WriteLine(string.Join(", ", model, version));
            Console.WriteLine("-------------------------------");
            Console.WriteLine(string.Join(",", MethodXMLFactory.GetFullInstalledVersions(model)));
            Console.WriteLine(string.Join(",", MethodXMLFactory.GetInstalledInstrumentModels()));
            Console.WriteLine(string.Join(",", MethodXMLFactory.GetInstalledServerModel()));
            Console.WriteLine(string.Join(",", MethodXMLFactory.GetInstalledServerVersion()));
            Console.WriteLine(string.Join(",", MethodXMLFactory.GetInstalledVersions(model)));
            Console.WriteLine(string.Join(",", MethodXMLFactory.GetLatestInstalledVersion(model)));

            Console.WriteLine("--------------------------------");
            return MethodXMLFactory.CreateContext(model, version);
        }

        /// <summary>
        /// Validates a .meth file for errors in parameters/settings
        /// </summary>
        /// <param name="methodFilePath">The method file path</param>
        /// <param name="model">The instrument model</param>
        /// <param name="version">The instrument version</param>
        /// <returns>A list of errors it detected, or an empty list if no errors are found</returns>
        public static List<string> Validate(string methodFilePath, string model = "", string version = "")
        {
            var validationErrors = new List<string>();
            methodFilePath = Path.GetFullPath(methodFilePath);
            using (IMethodXMLContext mxc = CreateContext(model, version))
            using (IMethodXML xmlMeth = mxc.Create())
            {
                xmlMeth.Open(methodFilePath);
                if (!xmlMeth.ValidateMethod())
                    validationErrors.AddRange(xmlMeth.GetLastValidationErrors());
            }
            return validationErrors;
        }

        /// <summary>
        /// Validates a .xml file for errors in its schema
        /// </summary>
        /// <param name="xmlFilePath">The xml file to validate</param>
        /// <param name="verion">The version of xsd to validate against</param>
        /// <returns>A list of errors it detected, or an empty list if no errors are found</returns>
        public static List<string> ValidateXML(string xmlFilePath, string version = "2.0")
        {
            // http://stackoverflow.com/questions/751511/validating-an-xml-against-referenced-xsd-in-c-sharp
            var validationErrors = new List<string>();

            if (string.IsNullOrEmpty(version))
            {
                version = "2.0"; // default to 2.0
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
            settings.ValidationEventHandler += (o, e) => { validationErrors.Add(string.Format("[Line {0}]: {1}", e.Exception.LineNumber, e.Message)); };

            InstrumentFamily family = GetInstrumentFamilyFromXml(xmlFilePath);

            string xsdFilePath;
            switch (family)
            {
                case InstrumentFamily.OrbitrapFusion:
                    xsdFilePath = @"XSDs\Calcium\{0}\MethodModifications.xsd";
                    break;

                case InstrumentFamily.TSQ:
                    xsdFilePath = @"XSDs\Hyperion\{0}\HyperionMethod.xsd";
                    break;

                default:
                    throw new ArgumentException("Cannot tell how to validate the xml file: " + xmlFilePath);
            }

            xsdFilePath = string.Format(xsdFilePath, version);

            settings.Schemas.Add(null, XmlReader.Create(xsdFilePath));

            XmlReader reader = XmlReader.Create(xmlFilePath, settings);

            while (reader.Read()) ;

            return validationErrors;
        }

        /// <summary>
        /// Applies the modification XML file to the method template, to produce a modified method file
        /// </summary>
        /// <param name="methodTemplate">The method to base off of</param>
        /// <param name="methodModXML">The modifications to be applied to the template method</param>
        /// <param name="outputMethod">The file path for the generated method</param>
        /// <param name="model">The instrument model</param>
        /// <param name="version">The instrument version</param>
        /// <param name="enableValidation">Enable automatic validation on saving</param>
        public static void ModifyMethod(string methodTemplate, string methodModXML, string outputMethod = "", string model = "", string version = "", bool enableValidation = true)
        {
            if (string.IsNullOrEmpty(methodTemplate))
                throw new ArgumentException("A method file path must be specified", "Method Template");

            if (string.IsNullOrEmpty(methodModXML))
                throw new ArgumentException("A method modification path must be specified", "Method Modification");

            // Handle relative/absolute paths
            methodTemplate = Path.GetFullPath(methodTemplate);
            methodModXML = Path.GetFullPath(methodModXML);

            // These files are required
            if (!File.Exists(methodTemplate))
                throw new IOException("File Not Found: " + methodTemplate);

            if (!File.Exists(methodModXML))
                throw new IOException("File Not Found: " + methodModXML);

            // Create output file path if not specified
            if (string.IsNullOrEmpty(outputMethod))
            {
                outputMethod = methodTemplate.Replace(".meth", "_modified.meth");
            }
            outputMethod = Path.GetFullPath(outputMethod);

            // Create output directory if doesn't exist
            if (!Directory.Exists(Path.GetDirectoryName(outputMethod)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputMethod));
            }

            // If the instrument model is not specified, get the default installed one. (might not be the best)
            if (string.IsNullOrEmpty(model))
            {
                model = MethodXMLFactory.GetInstalledServerModel();

                if (string.IsNullOrEmpty(model))
                {
                    var instruments = MethodXMLFactory.GetInstalledInstrumentModels();

                    if (instruments.Count > 1)
                    {
                        throw new Exception(string.Format("Unable to find default installed instrument, you have {0} instruments registered", instruments.Count));
                    }
                    else if (instruments.Count == 0)
                    {
                        throw new Exception("Unable to find any installed instruments!");
                    }
                    else
                    {
                        model = instruments[0];
                    }
                }
            }

            // Get the type of instrument based on its name
            InstrumentFamily instrumentFamily = GetInstrumentFamilyFromModel(model);

            // Get the type of instrument modification based on the xml file
            InstrumentFamily xmlInstrumentFamily = GetInstrumentFamilyFromXml(methodModXML);

            // These two need to be equivalent to correctly apply the modifications
            if (instrumentFamily != xmlInstrumentFamily)
            {
                throw new ArgumentException(string.Format("The specified xml ({0}) is not compatible with the instrument model ({1}, {2})", xmlInstrumentFamily, instrumentFamily, model));
            }

            using (IMethodXMLContext mxc = CreateContext(model, version))
            using (IMethodXML xmlMeth = mxc.Create())
            {
                // Open the template method
                xmlMeth.Open(methodTemplate);

                // Set the validation flag
                xmlMeth.EnableValidation(enableValidation);

                // Call the correct modification method based on the instrument type
                switch (instrumentFamily)
                {
                    case InstrumentFamily.OrbitrapFusion:
                        xmlMeth.ApplyMethodModificationsFromXMLFile(methodModXML);
                        break;

                    case InstrumentFamily.TSQ:
                        xmlMeth.ImportMassListFromXMLFile(methodModXML);
                        break;

                    case InstrumentFamily.Merkur:
                        xmlMeth.ApplyMethodModificationsFromXMLFile(methodModXML);
                        break;

                    default:
                        throw new ArgumentException("Unsupported instrument model:" + model);
                }

                // Save the in memory method to the output file
                xmlMeth.SaveAs(outputMethod);
            }
        }

        /// <summary>
        /// Creates a .meth file from .xml
        /// </summary>
        /// <param name="xmlFilePath">The xml file path to create the method from</param>
        /// <param name="outputMethod">The file path of the to be created method</param>
        /// <param name="model">The instrument model (only TSQ)</param>
        /// <param name="version">The instrument version</param>
        public static void CreateMethod(string xmlFilePath, string outputMethod = "", string model = "", string version = "")
        {
            // Handle relative/absolute paths
            xmlFilePath = Path.GetFullPath(xmlFilePath);


            if (string.IsNullOrEmpty(outputMethod))
            {
                outputMethod = Path.ChangeExtension(xmlFilePath, ".meth");
            }
            outputMethod = Path.GetFullPath(outputMethod);

            // Get the type of instrument based on its name
            InstrumentFamily instrumentFamily = GetInstrumentFamilyFromModel(model);
            Console.WriteLine(string.Join(", ", xmlFilePath, outputMethod, model, version, instrumentFamily));
            if (instrumentFamily != InstrumentFamily.TSQ)
            {
                throw new ArgumentException("You can only create methods from TSQ xml files");
            }

            using (IMethodXMLContext mxc = CreateContext(model, version))
            using (IMethodXML xmlMeth = mxc.Create())
            {
                // Loads the method from the xml
                xmlMeth.LoadMethodFromXMLFile(xmlFilePath);

                // Save the in memory method to the output file
                xmlMeth.SaveAs(outputMethod);
            }
        }



        /// <summary>
        /// Export a Hyperion method to xml
        /// </summary>
        /// <param name="inputMethod">Path to .meth file</param>
        /// <param name="exportXml">Path to output .xml file</param>
        /// <param name="model">The instrument model</param>
        /// <param name="version">The instrument version</param>
        public static void ExportMethod(string inputMethod, string exportXml = "", string model = "", string version = "")
        {
            inputMethod = Path.GetFullPath(inputMethod);

            if (string.IsNullOrEmpty(exportXml))
            {
                exportXml = Path.ChangeExtension(inputMethod, ".xml");
            }
            exportXml = Path.GetFullPath(exportXml);



            // Get the type of instrument based on its name
            InstrumentFamily instrumentFamily = GetInstrumentFamilyFromModel(model);
            if (instrumentFamily != InstrumentFamily.TSQ)
            {
                throw new ArgumentException("You can only export methods from TSQ method files");
            }



            using (IMethodXMLContext mxc = CreateContext(model, version))
            using (IMethodXML xmlMeth = mxc.Create())
            {
                xmlMeth.Open(inputMethod);
                xmlMeth.ExportMethodToXMLFile(exportXml);
            }
        }

        /// <summary>
        /// Determines the instrument family that the xml file intends to modify
        /// </summary>
        /// <param name="xmlFilePath">The xml file path of the modifications</param>
        /// <returns>The instrument family the xml file modifies</returns>
        public static InstrumentFamily GetInstrumentFamilyFromXml(string xmlFilePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);
            XmlElement root = doc.DocumentElement;
            string name = root.GetAttribute("Family");

            switch (name)
            {
                case "Calcium":
                    return InstrumentFamily.OrbitrapFusion;

                case "Hyperion":
                    return InstrumentFamily.TSQ;

                case "Merkur":
                    return InstrumentFamily.Merkur;

                default:
                    return InstrumentFamily.Unknown;
            }
        }

        /// <summary>
        /// Determines the instrument family based on the instrument model name
        /// </summary>
        /// <param name="instrumentModel">The model name of the instrument</param>
        /// <returns>The instrument family of the model name</returns>
        public static InstrumentFamily GetInstrumentFamilyFromModel(string instrumentModel)
        {
            if (instrumentModel.StartsWith("Orbitrap"))
            {
                return InstrumentFamily.OrbitrapFusion;
            }
            else if (instrumentModel.StartsWith("Merkur"))
            {
                return InstrumentFamily.Merkur;
            }
            else if (instrumentModel.StartsWith("TSQ"))
            {
                return InstrumentFamily.TSQ;
            }
            return InstrumentFamily.Unknown;
        }

        /// <summary>
        /// Get all the installed instrument model names
        /// </summary>
        /// <returns>The installed instrument names</returns>
        public static IEnumerable<string> GetInstalledInstrumentModels()
        {
            return MethodXMLFactory.GetInstalledInstrumentModels();
        }

        /// <summary>
        /// Prints information about the executing computer's TNG status
        /// </summary>
        public static IEnumerable<string> PrintInformation()
        {
            // Get all installed instruments for executing computer
            // Searches the registry for installed TNG instruments.
            // HKEY_LOCAL_MACHINE\SOFTWARE\Thermo Instruments\TNG
            // or
            // HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Thermo Instruments\TNG
            var instruments = MethodXMLFactory.GetInstalledInstrumentModels();

            yield return "== Configured Instruments ==";
            int i = 1;
            foreach (string instrument in instruments)
            {
                Console.WriteLine("{0,2}: {1}", i++, instrument);
                // Get all installed instrument versions for this particular instrument (i.e., Fusion 1.1, 1.2, etc...)
                var versions = MethodXMLFactory.GetInstalledVersions(instrument);

                // TNG can be installed as Full (connected to instrument) or Workstation (just MethodEditor) mode.
                var fullInstalledVersions = new HashSet<string>(MethodXMLFactory.GetFullInstalledVersions(instrument));

                // The most recently installed version for this instrument model
                var lastInstalledVersion = MethodXMLFactory.GetLatestInstalledVersion(instrument);

                foreach (string version in versions)
                {
                    yield return string.Format("{0,8} ({1}){2}",
                        version,
                        fullInstalledVersions.Contains(version) ? "Full" : "WorkStation",
                        version == lastInstalledVersion ? " Latest Installed" : "");
                }
            }
            yield return "============================";
            yield return string.Format("Installed Server Model:   {0}", MethodXMLFactory.GetInstalledServerModel());
            yield return string.Format("Installed Server Version: {0}", MethodXMLFactory.GetInstalledServerVersion());
        }
    }
}