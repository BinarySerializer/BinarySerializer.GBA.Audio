﻿using System;
using System.Linq;

namespace BinarySerializer.GBA.Audio.GAX
{
    public class GAX_Instrument : BinarySerializable {
        public byte Byte_00 { get; set; }
        public byte[] SampleIndices { get; set; }
        public byte Byte_05 { get; set; } // Padding?
        public byte Byte_06 { get; set; } // Padding?
        public byte Byte_07 { get; set; } // Padding?
        public byte Byte_08 { get; set; } // Used for additional pitch calculation
        public byte Byte_09 { get; set; } // Used for additional pitch calculation
        public byte Byte_0A { get; set; } // Used for additional pitch calculation
        public byte Byte_0B { get; set; }
        public Pointer<GAX_InstrumentEnvelope> Envelope { get; set; }
        public Pointer GAX2_Unknown { get; set; }
        public byte RowSpeed { get; set; } // After RowSpeed frames, it switches to the next row
        public byte NumRows { get; set; }
        public ushort UShort_12 { get; set; }
        public Pointer RowsPointer { get; set; }
        public GAX_InstrumentRow[] Rows { get; set; }
        public GAX_InstrumentSample[] Samples { get; set; }

        public override void SerializeImpl(SerializerObject s)
        {
            Byte_00 = s.Serialize<byte>(Byte_00, name: nameof(Byte_00));
            SampleIndices = s.SerializeArray<byte>(SampleIndices, 4, name: nameof(SampleIndices));
			Byte_05 = s.Serialize<byte>(Byte_05, name: nameof(Byte_05));
			Byte_06 = s.Serialize<byte>(Byte_06, name: nameof(Byte_06));
			Byte_07 = s.Serialize<byte>(Byte_07, name: nameof(Byte_07));
			Byte_08 = s.Serialize<byte>(Byte_08, name: nameof(Byte_08));
			Byte_09 = s.Serialize<byte>(Byte_09, name: nameof(Byte_09));
			Byte_0A = s.Serialize<byte>(Byte_0A, name: nameof(Byte_0A));
			Byte_0B = s.Serialize<byte>(Byte_0B, name: nameof(Byte_0B));
            if (s.GetGAXSettings().MajorVersion < 3) {
                Samples = s.SerializeObjectArray<GAX_InstrumentSample>(Samples, 4, name: nameof(Samples));
            }
            Envelope = s.SerializePointer<GAX_InstrumentEnvelope>(Envelope, resolve: true, name: nameof(Envelope));
            if (s.GetGAXSettings().MajorVersion < 3) {
				GAX2_Unknown = s.SerializePointer(GAX2_Unknown, name: nameof(GAX2_Unknown));
			}
            RowSpeed = s.Serialize<byte>(RowSpeed, name: nameof(RowSpeed));
			NumRows = s.Serialize<byte>(NumRows, name: nameof(NumRows));
			UShort_12 = s.Serialize<ushort>(UShort_12, name: nameof(UShort_12));
            RowsPointer = s.SerializePointer(RowsPointer, name: nameof(RowsPointer));
            s.DoAt(RowsPointer, () => {
				Rows = s.SerializeObjectArray<GAX_InstrumentRow>(Rows, NumRows, name: nameof(Rows));
			});
            if (s.GetGAXSettings().MajorVersion >= 3) {
                int numSamples = Math.Max(Rows.Max(k => k.SampleIndex), (byte)1);
                Samples = s.SerializeObjectArray<GAX_InstrumentSample>(Samples, numSamples, name: nameof(Samples));
            }
		}
    }
}