﻿using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

/// <summary>
/// This class runs R code from a file using the console.
/// </summary>
public class RScriptRunner
{
    /// <summary>
    /// Runs an R script from a file using Rscript.exe.
    /// Example:  
    ///   RScriptRunner.RunFromCmd(curDirectory + @"\ImageClustering.r", "rscript.exe", curDirectory.Replace('\\','/'));
    /// Getting args passed from C# using R:
    ///   args = commandArgs(trailingOnly = TRUE)
    ///   print(args[1]);
    /// </summary>
    /// <param name="rCodeFilePath">File where your R code is located.</param>
    /// <param name="rScriptExecutablePath">Usually only requires "rscript.exe"</param>
    /// <param name="args">Multiple R args can be seperated by spaces.</param>
    /// <returns>Returns a string with the R responses.</returns>
    public static string RunFromCmd(string rCodeFilePath, string rScriptExecutablePath, string args)
    {
        // string file = rCodeFilePath;
        string result = string.Empty;

        try
        {

            var info = new ProcessStartInfo();
            info.FileName = rScriptExecutablePath;
            info.WorkingDirectory = Path.GetDirectoryName(rScriptExecutablePath);
            info.Arguments = rCodeFilePath + " " + args;

            info.RedirectStandardInput = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            using (var proc = new Process())
            {
                proc.StartInfo = info;
                proc.Start();
                result = "\nSTDOUT:\n" + proc.StandardOutput.ReadToEnd() + "\nSTDERR:\n" + proc.StandardError.ReadToEnd() + "\n----------\n";
            }

            return result;
        }
        catch (Exception ex)
        {
			using (System.IO.StreamWriter writetofile =
				new System.IO.StreamWriter(Directory.GetCurrentDirectory() + "/Assets/Config/error.txt")){
				writetofile.WriteLine("R Script failed: " + ex);
				writetofile.Flush ();
				writetofile.Close ();
			}
			throw new Exception("R Script failed: " + result, ex);
        }
    }
}