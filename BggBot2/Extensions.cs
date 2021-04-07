﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BggBot2
{
    public static class Extensions
    {
        public static string GetId(this ClaimsPrincipal user)
        {
            return user.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value;
        }

        public static TV SafeGetValue<TK, TV>(this IDictionary<TK, TV> source, TK key)
        {
            return source.TryGetValue(key, out var result) ? result : default;
        }
    }

    public class NotFoundException : Exception { }
}