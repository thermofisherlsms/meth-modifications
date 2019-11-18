// /* Program.cs
//  *
//  * Copyright (C) 2016 Thermo Fisher Scientific
//  *
//  * This software may be modified and distributed under the terms
//  * of the MIT license.  See the LICENSE file for details.
//  */
using CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using XmlMethodChanger.lib;

namespace XmlMethodChanger.Cmd
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    if (options.GetInfo)
                    {
                        foreach (string info in MethodChanger.PrintInformation())
                            Console.WriteLine(info);
                    }
                    else if (options.GenerateSummary)
                    {
                        Console.WriteLine(MethodChanger.GetSummary(options.MethodTemplate, options.InstrumentModel, options.InstrumentVersion));
                    }
                    else if (!string.IsNullOrEmpty(options.Validate))
                    {
                        List<string> errors;

                        if (Path.GetExtension(options.Validate).Equals(".xml"))
                        {   
                            errors = MethodChanger.ValidateXML(options.Validate, options.InstrumentVersion);
                        }
                        else
                        {
                            errors = MethodChanger.Validate(options.Validate, options.InstrumentModel, options.InstrumentVersion);
                        }

                        Console.WriteLine();
                        if (errors.Count > 0)
                        {
                            Console.WriteLine("== Validation Errors Detected ==");
                            errors.ForEach(Console.WriteLine);
                            Console.WriteLine("================================");
                        }
                        else
                        {
                            Console.WriteLine("No Validation Errors Detected");
                        }                     
                    }
                    else if (!string.IsNullOrEmpty(options.CreateMethodXML))
                    {
                        MethodChanger.CreateMethod(options.CreateMethodXML, options.OutputFile, options.InstrumentModel, options.InstrumentVersion);
                    }
                    else if (!string.IsNullOrEmpty(options.ExportMethod))
                    {
                        MethodChanger.ExportMethod(options.ExportMethod, options.OutputFile, options.InstrumentModel, options.InstrumentVersion);
                    }
                    else
                    {
                        MethodChanger.ModifyMethod(options.MethodTemplate, options.MethodModification, options.OutputFile, options.InstrumentModel, options.InstrumentVersion);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(options.GetUsage());
                    Console.Error.WriteLine();
                    Console.Error.WriteLine("== Errors ==");
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine("============");
                }
            }
        }
    }
}