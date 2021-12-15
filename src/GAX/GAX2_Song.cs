﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BinarySerializer.GBA.Audio.GAX
{
    public class GAX2_Song : BinarySerializable
    {
        public int? PredefinedSampleCount { get; set; } // Set in onPreSerialize

        public ushort NumChannels { get; set; } // Actually a byte?
        public ushort NumRowsPerPattern { get; set; }
        public ushort NumPatternsPerChannel { get; set; }
        public ushort LoopPoint { get; set; } // Pattern index starting from 0
        public ushort Volume { get; set; }
        public ushort UShort_0A { get; set; }
        public Pointer SequenceDataPointer { get; set; }
        public Pointer InstrumentSetPointer { get; set; }
        public Pointer SampleSetPointer { get; set; }
        public ushort SampleRate { get; set; } // 0x3D99
        public ushort FXSampleRate { get; set; } // 0 for same as music
        public byte NumFXChannels { get; set; }
        public byte Byte_1D { get; set; }
        public ushort UShort_1E { get; set; }
        public Pointer[] PatternTablePointers { get; set; }
        public uint UInt_A0 { get; set; }
        public uint[] UInts_A4 { get; set; }
        public uint[] UInts_B0 { get; set; }
        public uint UInt_BC { get; set; }
        public uint UInt_C0 { get; set; }
        public uint UInt_C4 { get; set; }

        public string Name { get; set; }
        public string ParsedName { get; set; }
        public string ParsedArtist { get; set; }

        public GAX2_PatternHeader[][] PatternTable { get; set; }
        public GAX2_Pattern[][] Patterns { get; set; }
        public Pointer<GAX2_Instrument>[] InstrumentSet { get; set; }
        public int[] InstrumentIndices { get; set; }
        public GAX2_Sample[] Samples { get; set; }

        public override void SerializeImpl(SerializerObject s)
        {
            NumChannels = s.Serialize<ushort>(NumChannels, name: nameof(NumChannels));
            NumRowsPerPattern = s.Serialize<ushort>(NumRowsPerPattern, name: nameof(NumRowsPerPattern));
            NumPatternsPerChannel = s.Serialize<ushort>(NumPatternsPerChannel, name: nameof(NumPatternsPerChannel));
            LoopPoint = s.Serialize<ushort>(LoopPoint, name: nameof(LoopPoint));
            Volume = s.Serialize<ushort>(Volume, name: nameof(Volume));
            UShort_0A = s.Serialize<ushort>(UShort_0A, name: nameof(UShort_0A));
            SequenceDataPointer = s.SerializePointer(SequenceDataPointer, name: nameof(SequenceDataPointer));
            InstrumentSetPointer = s.SerializePointer(InstrumentSetPointer, name: nameof(InstrumentSetPointer));
            SampleSetPointer = s.SerializePointer(SampleSetPointer, name: nameof(SampleSetPointer));
            SampleRate = s.Serialize<ushort>(SampleRate, name: nameof(SampleRate));
            FXSampleRate = s.Serialize<ushort>(FXSampleRate, name: nameof(FXSampleRate));
			NumFXChannels = s.Serialize<byte>(NumFXChannels, name: nameof(NumFXChannels));
			Byte_1D = s.Serialize<byte>(Byte_1D, name: nameof(Byte_1D));
			UShort_1E = s.Serialize<ushort>(UShort_1E, name: nameof(UShort_1E));
            PatternTablePointers = s.SerializePointerArray(PatternTablePointers, 32, name: nameof(PatternTablePointers));
			UInt_A0 = s.Serialize<uint>(UInt_A0, name: nameof(UInt_A0));
			UInts_A4 = s.SerializeArray<uint>(UInts_A4, 3, name: nameof(UInts_A4));
			UInts_B0 = s.SerializeArray<uint>(UInts_B0, 3, name: nameof(UInts_B0));
			UInt_BC = s.Serialize<uint>(UInt_BC, name: nameof(UInt_BC));
			UInt_C0 = s.Serialize<uint>(UInt_C0, name: nameof(UInt_C0));
			UInt_C4 = s.Serialize<uint>(UInt_C4, name: nameof(UInt_C4));

			List<int> instruments = new List<int>();
            if (PatternTable == null) {
                int instrumentCount = 0;
                PatternTable = new GAX2_PatternHeader[NumChannels][];
                Patterns = new GAX2_Pattern[NumChannels][];
                for (int i = 0; i < NumChannels; i++) {
                    s.DoAt(PatternTablePointers[i], () => {
                        PatternTable[i] = s.SerializeObjectArray<GAX2_PatternHeader>(PatternTable[i], NumPatternsPerChannel, name: $"{nameof(PatternTable)}[{i}]");
                        if (Patterns[i] == null) {
                            Patterns[i] = new GAX2_Pattern[PatternTable[i].Length];
                            for (int j = 0; j < Patterns[i].Length; j++) {
                                s.DoAt(SequenceDataPointer + PatternTable[i][j].SequenceOffset, () => {
                                    Patterns[i][j] = s.SerializeObject<GAX2_Pattern>(Patterns[i][j], onPreSerialize: t => t.Duration = NumRowsPerPattern, name: $"{nameof(Patterns)}[{i}][{j}]");
                                    if (Patterns[i][j].Rows.Length > 0) {
                                        instrumentCount = Math.Max(instrumentCount, Patterns[i][j].Rows
                                            .Max(cmd => (cmd.Command == GAX2_PatternRow.Cmd.Note || cmd.Command == GAX2_PatternRow.Cmd.NoteOnly) ? cmd.Instrument + 1 : 0));
                                        instruments.AddRange(Patterns[i][j].Rows
                                            .Where(cmd => cmd.Command == GAX2_PatternRow.Cmd.Note || cmd.Command == GAX2_PatternRow.Cmd.NoteOnly)
                                            .Select(cmd => (int)cmd.Instrument));
                                    }
                                });
                            }
                        }
                    });
                }
                InstrumentIndices = instruments.Distinct().Where(i => i != 250).ToArray();
                s.Log("Instrument Count: " + InstrumentIndices.Length);
                Pointer endOffset = Patterns.Max(ta => ta.Max(t => t.EndOffset));
                s.DoAt(endOffset, () => {
                    Name = s.Serialize<string>(Name, name: nameof(Name));
                    string[] parse = Name.Split('"');
                    ParsedName = parse[1];
                    ParsedArtist = parse[2].Substring(3, 0xF);
                    s.Log(ParsedName + " - " + ParsedArtist);
                });
                s.DoAt(InstrumentSetPointer, () => {
                    InstrumentSet = s.SerializePointerArray<GAX2_Instrument>(InstrumentSet, PredefinedSampleCount ?? instrumentCount, resolve: true, name: nameof(InstrumentSet));
                });
                Samples = new GAX2_Sample[PredefinedSampleCount ?? InstrumentIndices.Length];
                for (int i = 0; i < Samples.Length; i++) {
                    int ind = PredefinedSampleCount.HasValue ? i : InstrumentIndices[i];
                    var instr = InstrumentSet[ind].Value;
                    if (instr != null) {
                        s.DoAt(SampleSetPointer + (instr.SampleIndices[0]) * 8, () => {
                            Samples[i] = s.SerializeObject<GAX2_Sample>(Samples[i], name: $"{nameof(Samples)}[{i}]");
                        });
                    }
                }
            }
        }
    }
}