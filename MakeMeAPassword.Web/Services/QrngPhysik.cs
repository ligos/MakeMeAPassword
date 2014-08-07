// Copyright 2014 Murray Grant
//
//    Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Methods to call the QRNG at qrng.physik.hu-berlin.de
    /// </summary>
    public sealed class QrngPhysik
    {
        // Mostly taken from https://qrng.physik.hu-berlin.de/download/Csharp-demo-Windows-32bit.zip

        public enum _qrng_error
        {
            QRNG_SUCCESS, // = 0 
            QRNG_ERR_FAILED_TO_BASE_INIT,
            QRNG_ERR_FAILED_TO_INIT_SOCK,
            QRNG_ERR_FAILED_TO_INIT_SSL,
            QRNG_ERR_FAILED_TO_CONNECT,
            QRNG_ERR_SERVER_FAILED_TO_INIT_SSL,
            QRNG_ERR_FAILED_SSL_HANDSHAKE,
            QRNG_ERR_DURING_USER_AUTH,
            QRNG_ERR_USER_CONNECTION_QUOTA_EXCEEDED,
            QRNG_ERR_BAD_CREDENTIALS,
            QRNG_ERR_NOT_CONNECTED,
            QRNG_ERR_BAD_BYTES_COUNT,
            QRNG_ERR_BAD_BUFFER_ADDRESS,
            QRNG_ERR_PASSWORD_CHARLIST_TOO_LONG,
            QRNG_ERR_READING_RANDOM_DATA_FAILED_ZERO,
            QRNG_ERR_READING_RANDOM_DATA_FAILED_INCOMPLETE,
            QRNG_ERR_READING_RANDOM_DATA_OVERFLOW,
            QRNG_ERR_FAILED_TO_READ_WELCOMEMSG,
            QRNG_ERR_FAILED_TO_READ_AUTH_REPLY,
            QRNG_ERR_FAILED_TO_READ_USER_REPLY,
            QRNG_ERR_FAILED_TO_READ_PASS_REPLY,
            QRNG_ERR_FAILED_TO_SEND_COMMAND
            // you may obtain between 1 to 2147483647 bytes with one get_random_bytes() call*/  });    
        }

        public static readonly string[] qrng_error_strings = {
    	    "QRNG_SUCCESS",
	        "QRNG_ERR_FAILED_TO_BASE_INIT",
	        "QRNG_ERR_FAILED_TO_INIT_SOCK",
	        "QRNG_ERR_FAILED_TO_INIT_SSL",
	        "QRNG_ERR_FAILED_TO_CONNECT",
	        "QRNG_ERR_SERVER_FAILED_TO_INIT_SSL",
	        "QRNG_ERR_FAILED_SSL_HANDSHAKE",
	        "QRNG_ERR_DURING_USER_AUTH",
	        "QRNG_ERR_USER_CONNECTION_QUOTA_EXCEEDED",
	        "QRNG_ERR_BAD_CREDENTIALS",
	        "QRNG_ERR_NOT_CONNECTED",
	        "QRNG_ERR_BAD_BYTES_COUNT",
	        "QRNG_ERR_BAD_BUFFER_ADDRESS",
	        "QRNG_ERR_PASSWORD_CHARLIST_TOO_LONG",
	        "QRNG_ERR_READING_RANDOM_DATA_FAILED_ZERO",
	        "QRNG_ERR_READING_RANDOM_DATA_FAILED_INCOMPLETE",
	        "QRNG_ERR_READING_RANDOM_DATA_OVERFLOW",
	        "QRNG_ERR_FAILED_TO_READ_WELCOMEMSG",
	        "QRNG_ERR_FAILED_TO_READ_AUTH_REPLY",
	        "QRNG_ERR_FAILED_TO_READ_USER_REPLY",
	        "QRNG_ERR_FAILED_TO_READ_PASS_REPLY",
	        "QRNG_ERR_FAILED_TO_SEND_COMMAND"
        };

        private bool QRNGDLLLoaded = false;
        public bool CheckDLL()
        {
            QRNGDLLLoaded = true;
            return QRNGDLLLoaded;
        }

        // Note, there are other functions for this which aren't included.


        //{+// connect to QRNG service first, by default no ssl will be used*/ }
        [DllImport("libqrng.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 qrng_connect(string username,
                                                string password);

        //{+// disconnect*/ }
        [DllImport("libqrng.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 qrng_disconnect();


        //{+// read bytes / double(s) / int(s) (requires an established connection) }
        //{-make sure your program allocated the value / array beforehand }
        //{=if connected via SSL, the data will be also encrypted }
        [DllImport("libqrng.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 qrng_get_byte_array([In][Out]byte[] byte_array,
                                                       Int32 byte_array_size,
                                                       out Int32 actual_bytes_rcvd);


        //{+// here are some handy one-liner functions which automatically a) connect to the QRNG service, }
        //{-b) retrieve the requested data and c) disconnect again. }
        //{-You can use them, if you retrieve data only once in a while. }
        //{=(By default no SSL will be used.) }
        [DllImport("libqrng.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 qrng_connect_and_get_byte_array(string username,
                                                                   string password,
                                                                   [In][Out]byte[] byte_array,
                                                                   Int32 byte_array_size,
                                                                   out Int32 actual_bytes_rcvd);



    }
}