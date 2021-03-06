﻿using System;
using System.Linq;
using Hystrix.Dotnet.ConcurrencyUtilities;

namespace Hystrix.Dotnet
{
    internal class RollingNumberBucket
    {
        public long WindowStart { get; }

        private readonly StripedLongAdder[] adderForCounterType;
        private readonly LongMaxUpdater[] updaterForCounterType;

        public RollingNumberBucket(long startTime)
        {
            WindowStart = startTime;

            /*
             * We support both LongAdder and LongMaxUpdater in a bucket but don't want the memory allocation
             * of all types for each so we only allocate the objects if the HystrixRollingNumberEvent matches
             * the correct type - though we still have the allocation of empty arrays to the given length
             * as we want to keep using the type.ordinal() value for fast random access.
             */

            var values = Enum.GetValues(typeof(HystrixRollingNumberEvent)).Cast<HystrixRollingNumberEvent>().ToArray();

            adderForCounterType = new StripedLongAdder[values.Length];
            foreach (var value in values)
            {
                if (value.IsCounter())
                {
                    adderForCounterType[(int)value] = new StripedLongAdder();
                }
            }

            updaterForCounterType = new LongMaxUpdater[values.Length];
            foreach (var value in values)
            {
                if (value.IsMaxUpdater())
                {
                    updaterForCounterType[(int)value] = new LongMaxUpdater();
                    // initialize to 0 otherwise it is Long.MIN_VALUE
                    updaterForCounterType[(int)value].Update(0);
                }
            }
        }

        public long Get(HystrixRollingNumberEvent type)
        {
            if (type.IsCounter())
            {
                return adderForCounterType[(int)type].GetValue();
            }
            if (type.IsMaxUpdater())
            {
                return updaterForCounterType[(int)type].Max();
            }

            throw new InvalidOperationException("Unknown type of event: " + type);
        }

        public StripedLongAdder GetAdder(HystrixRollingNumberEvent type)
        {
            if (!type.IsCounter())
            {
                throw new InvalidOperationException("Type is not a Counter: " + type);
            }

            return adderForCounterType[(int)type];
        }

        public LongMaxUpdater GetMaxUpdater(HystrixRollingNumberEvent type)
        {
            if (!type.IsMaxUpdater())
            {
                throw new InvalidOperationException("Type is not a MaxUpdater: " + type);
            }

            return updaterForCounterType[(int)type];
        }
    }
}
