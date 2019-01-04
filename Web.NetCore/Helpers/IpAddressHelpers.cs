// Copyright 2019 Murray Grant
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
using System.Net;

using Microsoft.Extensions.ObjectPool;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers
{
    public static class IpAddressHelpers
    {
        public static readonly IPAddress V6DefaultSubnetMask = new IPAddress(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
        private static readonly ReadOnlyMemory<byte> _V6DefaultSubnetMask = V6DefaultSubnetMask.GetAddressBytes();

        /// <summary>
        /// Get an IPv6 Network address. Assumes /64.
        /// </summary>
        public static IPAddress V6NetworkAddress(this IPAddress address, IPAddress subnetMask = null)
        {
            if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                throw new ArgumentException("Not an IPv6 address: " + address, nameof(address));
            var snm = subnetMask ?? V6DefaultSubnetMask;
            if (snm.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                throw new ArgumentException("Not an IPv6 netmask: " + snm, nameof(subnetMask));

            // Yay for playing with Span<T> and family!
            // Unfortunately, because you can't get at the raw bytes inside an IPAddress object, we end up making several copies here anyway.
            var ipAddressSpan = address.GetAddressBytes().AsSpan();
            var subnetSpan = (Object.ReferenceEquals(V6DefaultSubnetMask, subnetMask) ? _V6DefaultSubnetMask : snm.GetAddressBytes()).Span;
            var broadcastSpan = new byte[ipAddressSpan.Length].AsSpan();
            for (int i = 0; i < broadcastSpan.Length; i++)
            {
                broadcastSpan[i] = (byte)(ipAddressSpan[i] & (subnetSpan[i]));
            }
            return new IPAddress(broadcastSpan);
        }
    }
}