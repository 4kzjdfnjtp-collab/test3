using Kermalis.EndianBinaryIO;
using Kermalis.MIDI;
using System;
using System.Text;

namespace FLP;

/// <summary>
/// Optional static (non-automated) values for the extra MIDI CCs exposed as generic knobs
/// on the "MIDI Out" plugin's "Basic controllers" / "Boolean controllers" pages.
/// A null field means "leave FL Studio's own default for that knob" (i.e. the MIDI never set that CC).
/// NOTE: These are only baked in as constant knob values. True time-varying automation for these
/// CCs (i.e. the CC changes value more than once over the course of the track) is not yet supported;
/// when that happens, the LAST value seen is used as a constant fallback instead.
/// </summary>
public struct MIDIOutExtraCCs
{
	public byte? Modulation; // CC1
	public byte? Breath; // CC2
	public byte? Foot; // CC4
	public byte? PortamentoTime; // CC5
	public byte? ReverbSend; // CC91
	public byte? ChorusSend; // CC93
	public byte? Sustain; // CC64 (damper/sustain pedal)
	public byte? Portamento; // CC65 (portamento on/off)
	public byte? Sostenuto; // CC66
	public byte? SoftPedal; // CC67
	public byte? Legato; // CC68
	public byte? Hold2; // CC69

	public readonly bool HasAnyValue =>
		Modulation.HasValue || Breath.HasValue || Foot.HasValue || PortamentoTime.HasValue
		|| ReverbSend.HasValue || ChorusSend.HasValue || Sustain.HasValue || Portamento.HasValue
		|| Sostenuto.HasValue || SoftPedal.HasValue || Legato.HasValue || Hold2.HasValue;
}

internal struct FLPluginParams
{
	// This is the 935-byte tail of a real "MIDI Out" plugin's PluginParams event, taken from an FL Studio
	// 26.1.6.5406 export. It defines FL's own default "Basic controllers"/"Boolean controllers" pages
	// (Mod, Breath, Foot, Port. Time, Reverb, Chorus, Damper, Porta., Sustenuto, Soft pedal, Legato, Hold 2)
	// plus 6 empty placeholder pages ("Page 3".."Page 8"). We patch specific bytes in a clone of this
	// template to set constant values for the CCs we know about; everything else (names, ranges, the
	// 6 unused/disabled generic knob slots per page, etc.) is left exactly as FL Studio itself writes it.
	private static readonly byte[] s_midiOutTemplate = Convert.FromHexString(
		"0000000000000000000000000000020000000000000000000000000000000000000000000000000000007f00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000100000000000000000000007f000000ffffffff00000000104d6f64756c6174696f6e20776865656c034d6f64010200008000000000000000007f000000ffffffff000000000e42726561746820636f6e74726f6c06427265617468020400008000000000000000007f000000ffffffff000000000f466f6f7420636f6e74726f6c6c657204466f6f74030500000000000000000000007f000000ffffffff000000000f506f7274616d656e746f2074696d650a506f72742e2054696d65045b00008000000000000000007f000000ffffffff00000000115265766572622073656e64206c6576656c06526576657262055d00008000000000000000007f000000ffffffff000000001143686f7275732073656e64206c6576656c0643686f727573094000008000000000000000007f000000ffffffff000000000c44616d70657220706564616c0644616d7065720a4100000000000000000000007f000000ffffffff000000000a506f7274616d656e746f06506f7274612e0b4200008000000000000000007f000000ffffffff000000000953757374656e75746f0953757374656e75746f0c4300008000000000000000007f000000ffffffff000000000a536f667420706564616c0a536f667420706564616c0d4400008000000000000000007f000000ffffffff00000000114c656761746f20466f6f74737769746368064c656761746f0e4500008000000000000000007f000000ffffffff0000000006486f6c64203206486f6c642032ff11426173696320636f6e74726f6c6c65727313426f6f6c65616e20636f6e74726f6c6c65727306506167652033065061676520340650616765203506506167652036065061676520370650616765203800000000");

	public static void WriteMIDIOut(EndianBinaryWriter w, byte midiChannel, byte midiBank, MIDIProgram program, MIDIOutExtraCCs extraCCs = default)
	{
		w.WriteEnum(FLEvent.PluginParams);
		FLProjectWriter.WriteArrayEventLength(w, (uint)(32 + s_midiOutTemplate.Length));

		w.WriteUInt32(7);
		w.WriteUInt32(midiChannel);
		w.WriteInt32(-1);
		w.WriteInt32(-1);
		w.WriteInt32(-1);
		w.WriteInt32(extraCCs.HasAnyValue ? 1 : 0);
		w.WriteInt32(midiBank);

		w.WriteUInt16(1);
		w.WriteUInt16((byte)(program + 1));

		byte[] body = (byte[])s_midiOutTemplate.Clone();
		Patch(body, 2, extraCCs.Modulation);
		Patch(body, 6, extraCCs.Breath);
		Patch(body, 10, extraCCs.Foot);
		Patch(body, 14, extraCCs.PortamentoTime);
		Patch(body, 18, extraCCs.ReverbSend);
		Patch(body, 22, extraCCs.ChorusSend);
		Patch(body, 38, extraCCs.Sustain);
		Patch(body, 42, extraCCs.Portamento);
		Patch(body, 46, extraCCs.Sostenuto);
		Patch(body, 50, extraCCs.SoftPedal);
		Patch(body, 54, extraCCs.Legato);
		Patch(body, 58, extraCCs.Hold2);
		w.WriteBytes(body);
	}

	private static void Patch(byte[] body, int offset, byte? value)
	{
		if (value.HasValue)
		{
			body[offset] = value.Value;
		}
	}

	public static void WriteFruityLSD(EndianBinaryWriter w, byte bankID, string dlsPath)
	{
		byte[] pathBytes = Encoding.UTF8.GetBytes(dlsPath);
		byte dlsPathLen = (byte)pathBytes.Length;

		w.WriteEnum(FLEvent.PluginParams);
		FLProjectWriter.WriteArrayEventLength(w, (uint)(97 + dlsPathLen));

		w.WriteUInt32(0);
		w.WriteUInt32(0x80); // 128
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(bankID);

		w.WriteByte(dlsPathLen); // It's possible this is a varLen length, but I didn't check
		w.WriteBytes(pathBytes);

		w.WriteZeroes(7);

		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0x80); // 128
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteUInt32(0);
		w.WriteByte(0);
	}
}
