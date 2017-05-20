//licHeader
//===============================================================================================================
// System  : Nistec.Channels - Nistec.Channels Class Library
// Author  : Nissim Trujman  (nissim@nistec.net)
// Updated : 01/07/2015
// Note    : Copyright 2007-2015, Nissim Trujman, All rights reserved
// Compiler: Microsoft Visual C#
//
// This file contains a class that is part of nistec library.
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: http://nistec.net/license/nistec.cache-license.txt.  
// This notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who      Comments
// ==============================================================================================================
// 10/01/2006  Nissim   Created the code
//===============================================================================================================
//licHeader|
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;

namespace Nistec.Channels
{

    
    /// <summary><see cref="AppDomainStarter.Start"/> starts an AppDomain.</summary>
    public static class AppDomainStarter
    {
        /// <summary>Creates a type in a new sandbox-friendly AppDomain.</summary>
        /// <typeparam name="T">A trusted type derived MarshalByRefObject to create 
        /// in the new AppDomain. The constructor of this type must catch any 
        /// untrusted exceptions so that no untrusted exception can escape the new 
        /// AppDomain.</typeparam>
        /// <param name="baseFolder">Value to use for AppDomainSetup.ApplicationBase.
        /// The AppDomain will be able to use any assemblies in this folder.</param>
        /// <param name="appDomainName">A friendly name for the AppDomain. MSDN
        /// does not state whether or not the name must be unique.</param>
        /// <param name="constructorArgs">Arguments to send to the constructor of T,
        /// or null to call the default constructor. Do not send arguments of 
        /// untrusted types this way.</param>
        /// <param name="partialTrust">Whether the new AppDomain should run in 
        /// partial-trust mode.</param>
        /// <returns>A remote proxy to an instance of type T. You can call methods 
        /// of T and the calls will be marshalled across the AppDomain boundary.</returns>
        public static T Start<T>(string baseFolder, string appDomainName,
            object[] constructorArgs, bool partialTrust)
            where T : MarshalByRefObject
        {
            // With help from http://msdn.microsoft.com/en-us/magazine/cc163701.aspx
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = baseFolder;

            AppDomain newDomain;
            if (partialTrust)
            {
                var permSet = new PermissionSet(PermissionState.None);
                permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                permSet.AddPermission(new UIPermission(PermissionState.Unrestricted));
                newDomain = AppDomain.CreateDomain(appDomainName, null, setup, permSet);
            }
            else
            {
                newDomain = AppDomain.CreateDomain(appDomainName, null, setup);
            }
            return (T)Activator.CreateInstanceFrom(newDomain,
                typeof(T).Assembly.ManifestModule.FullyQualifiedName,
                typeof(T).FullName, false,
                0, null, constructorArgs, null, null).Unwrap();
        }
    }
}
