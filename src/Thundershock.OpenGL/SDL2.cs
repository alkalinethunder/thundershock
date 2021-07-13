#region License
/* SDL2# - C# Wrapper for SDL2
 *
 * Copyright (c) 2013-2021 Ethan Lee.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */
#endregion

#region Using Statements

using System;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Thundershock.OpenGL
{
	public static class Sdl
	{
		#region SDL2# Variables

		private const string NativeLibName = "SDL2";

		#endregion

		#region UTF8 Marshaling

		/* Used for stack allocated string marshaling. */
		internal static int Utf8Size(string str)
		{
			if (str == null)
			{
				return 0;
			}
			return (str.Length * 4) + 1;
		}
		internal static unsafe byte* Utf8Encode(string str, byte* buffer, int bufferSize)
		{
			if (str == null)
			{
				return (byte*) 0;
			}
			fixed (char* strPtr = str)
			{
				Encoding.UTF8.GetBytes(strPtr, str.Length + 1, buffer, bufferSize);
			}
			return buffer;
		}

		/* Used for heap allocated string marshaling.
		 * Returned byte* must be free'd with FreeHGlobal.
		 */
		internal static unsafe byte* Utf8EncodeHeap(string str)
		{
			if (str == null)
			{
				return (byte*) 0;
			}

			int bufferSize = Utf8Size(str);
			byte* buffer = (byte*) Marshal.AllocHGlobal(bufferSize);
			fixed (char* strPtr = str)
			{
				Encoding.UTF8.GetBytes(strPtr, str.Length + 1, buffer, bufferSize);
			}
			return buffer;
		}

		/* This is public because SDL_DropEvent needs it! */
		public static unsafe string UTF8_ToManaged(IntPtr s, bool freePtr = false)
		{
			if (s == IntPtr.Zero)
			{
				return null;
			}

			/* We get to do strlen ourselves! */
			byte* ptr = (byte*) s;
			while (*ptr != 0)
			{
				ptr++;
			}

			/* TODO: This #ifdef is only here because the equivalent
			 * .NET 2.0 constructor appears to be less efficient?
			 * Here's the pretty version, maybe steal this instead:
			 *
			string result = new string(
				(sbyte*) s, // Also, why sbyte???
				0,
				(int) (ptr - (byte*) s),
				System.Text.Encoding.UTF8
			);
			 * See the CoreCLR source for more info.
			 * -flibit
			 */
#if NETSTANDARD2_0
			/* Modern C# lets you just send the byte*, nice! */
			string result = System.Text.Encoding.UTF8.GetString(
				(byte*) s,
				(int) (ptr - (byte*) s)
			);
#else
			/* Old C# requires an extra memcpy, bleh! */
			int len = (int) (ptr - (byte*) s);
			if (len == 0)
			{
				return string.Empty;
			}
			char* chars = stackalloc char[len];
			int strLen = Encoding.UTF8.GetChars((byte*) s, len, chars, len);
			string result = new string(chars, 0, strLen);
#endif

			/* Some SDL functions will malloc, we have to free! */
			if (freePtr)
			{
				SDL_free(s);
			}
			return result;
		}

		#endregion

		#region SDL_stdinc.h

		public static uint SDL_FOURCC(byte a, byte b, byte c, byte d)
		{
			return (uint) (a | (b << 8) | (c << 16) | (d << 24));
		}

		public enum SdlBool
		{
			SdlFalse = 0,
			SdlTrue = 1
		}

		/* malloc/free are used by the marshaler! -flibit */

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr SDL_malloc(IntPtr size);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void SDL_free(IntPtr memblock);

		/* Buffer.BlockCopy is not available in every runtime yet. Also,
		 * using memcpy directly can be a compatibility issue in other
		 * strange ways. So, we expose this to get around all that.
		 * -flibit
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_memcpy(IntPtr dst, IntPtr src, IntPtr len);

		#endregion

		#region SDL_rwops.h

		public const int RwSeekSet = 0;
		public const int RwSeekCur = 1;
		public const int RwSeekEnd = 2;

		public const UInt32 SdlRwopsUnknown	= 0; /* Unknown stream type */
		public const UInt32 SdlRwopsWinfile	= 1; /* Win32 file */
		public const UInt32 SdlRwopsStdfile	= 2; /* Stdio file */
		public const UInt32 SdlRwopsJnifile	= 3; /* Android asset */
		public const UInt32 SdlRwopsMemory	= 4; /* Memory stream */
		public const UInt32 SdlRwopsMemoryRo = 5; /* Read-Only memory stream */

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long SdlrWopsSizeCallback(IntPtr context);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long SdlrWopsSeekCallback(
			IntPtr context,
			long offset,
			int whence
		);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr SdlrWopsReadCallback(
			IntPtr context,
			IntPtr ptr,
			IntPtr size,
			IntPtr maxnum
		);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr SdlrWopsWriteCallback(
			IntPtr context,
			IntPtr ptr,
			IntPtr size,
			IntPtr num
		);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int SdlrWopsCloseCallback(
			IntPtr context
		);

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlRWops
		{
			public IntPtr size;
			public IntPtr seek;
			public IntPtr read;
			public IntPtr write;
			public IntPtr close;

			public UInt32 type;

			/* NOTE: This isn't the full structure since
			 * the native SDL_RWops contains a hidden union full of
			 * internal information and platform-specific stuff depending
			 * on what conditions the native library was built with
			 */
		}

		/* IntPtr refers to an SDL_RWops* */
		[DllImport(NativeLibName, EntryPoint = "SDL_RWFromFile", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe IntPtr INTERNAL_SDL_RWFromFile(
			byte* file,
			byte* mode
		);
		public static unsafe IntPtr SDL_RWFromFile(
			string file,
			string mode
		) {
			byte* utf8File = Utf8EncodeHeap(file);
			byte* utf8Mode = Utf8EncodeHeap(mode);
			IntPtr rwOps = INTERNAL_SDL_RWFromFile(
				utf8File,
				utf8Mode
			);
			Marshal.FreeHGlobal((IntPtr) utf8Mode);
			Marshal.FreeHGlobal((IntPtr) utf8File);
			return rwOps;
		}

		/* IntPtr refers to an SDL_RWops* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_AllocRW();

		/* area refers to an SDL_RWops* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeRW(IntPtr area);

		/* fp refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_RWFromFP(IntPtr fp, SdlBool autoclose);

		/* mem refers to a void*, IntPtr to an SDL_RWops* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_RWFromMem(IntPtr mem, int size);

		/* mem refers to a const void*, IntPtr to an SDL_RWops* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_RWFromConstMem(IntPtr mem, int size);

		/* context refers to an SDL_RWops*.
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern long SDL_RWsize(IntPtr context);

		/* context refers to an SDL_RWops*.
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern long SDL_RWseek(
			IntPtr context,
			long offset,
			int whence
		);

		/* context refers to an SDL_RWops*.
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern long SDL_RWtell(IntPtr context);

		/* context refers to an SDL_RWops*, ptr refers to a void*.
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern long SDL_RWread(
			IntPtr context,
			IntPtr ptr,
			IntPtr size,
			IntPtr maxnum
		);

		/* context refers to an SDL_RWops*, ptr refers to a const void*.
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern long SDL_RWwrite(
			IntPtr context,
			IntPtr ptr,
			IntPtr size,
			IntPtr maxnum
		);

		/* Read endian functions */

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern byte SDL_ReadU8(IntPtr src);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt16 SDL_ReadLE16(IntPtr src);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt16 SDL_ReadBE16(IntPtr src);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_ReadLE32(IntPtr src);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_ReadBE32(IntPtr src);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt64 SDL_ReadLE64(IntPtr src);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt64 SDL_ReadBE64(IntPtr src);

		/* Write endian functions */

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WriteU8(IntPtr dst, byte value);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WriteLE16(IntPtr dst, UInt16 value);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WriteBE16(IntPtr dst, UInt16 value);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WriteLE32(IntPtr dst, UInt32 value);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WriteBE32(IntPtr dst, UInt32 value);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WriteLE64(IntPtr dst, UInt64 value);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WriteBE64(IntPtr dst, UInt64 value);

		/* context refers to an SDL_RWops*
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern long SDL_RWclose(IntPtr context);

		/* datasize refers to a size_t*
		 * IntPtr refers to a void*
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_LoadFile", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe IntPtr INTERNAL_SDL_LoadFile(byte* file, out IntPtr datasize);
		public static unsafe IntPtr SDL_LoadFile(string file, out IntPtr datasize)
		{
			byte* utf8File = Utf8EncodeHeap(file);
			IntPtr result = INTERNAL_SDL_LoadFile(utf8File, out datasize);
			Marshal.FreeHGlobal((IntPtr) utf8File);
			return result;
		}

		#endregion

		#region SDL_main.h

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetMainReady();

		/* This is used as a function pointer to a C main() function */
		public delegate int SdlMainFunc(int argc, IntPtr argv);

		/* Use this function with UWP to call your C# Main() function! */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_WinRTRunApp(
			SdlMainFunc mainFunction,
			IntPtr reserved
		);

		/* Use this function with iOS to call your C# Main() function!
		 * Only available in SDL 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UIKitRunApp(
			int argc,
			IntPtr argv,
			SdlMainFunc mainFunction
		);

		#endregion

		#region SDL.h

		public const uint SdlInitTimer =		0x00000001;
		public const uint SdlInitAudio =		0x00000010;
		public const uint SdlInitVideo =		0x00000020;
		public const uint SdlInitJoystick =		0x00000200;
		public const uint SdlInitHaptic =		0x00001000;
		public const uint SdlInitGamecontroller =	0x00002000;
		public const uint SdlInitEvents =		0x00004000;
		public const uint SdlInitSensor =		0x00008000;
		public const uint SdlInitNoparachute =	0x00100000;
		public const uint SdlInitEverything = (
			SdlInitTimer | SdlInitAudio | SdlInitVideo |
			SdlInitEvents | SdlInitJoystick | SdlInitHaptic |
			SdlInitGamecontroller | SdlInitSensor
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_Init(uint flags);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_InitSubSystem(uint flags);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_Quit();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_QuitSubSystem(uint flags);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_WasInit(uint flags);

		#endregion

		#region SDL_platform.h

		[DllImport(NativeLibName, EntryPoint = "SDL_GetPlatform", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetPlatform();
		public static string SDL_GetPlatform()
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetPlatform());
		}

		#endregion

		#region SDL_hints.h

		public const string SdlHintFramebufferAcceleration =
			"SDL_FRAMEBUFFER_ACCELERATION";
		public const string SdlHintRenderDriver =
			"SDL_RENDER_DRIVER";
		public const string SdlHintRenderOpenglShaders =
			"SDL_RENDER_OPENGL_SHADERS";
		public const string SdlHintRenderDirect3DThreadsafe =
			"SDL_RENDER_DIRECT3D_THREADSAFE";
		public const string SdlHintRenderVsync =
			"SDL_RENDER_VSYNC";
		public const string SdlHintVideoX11Xvidmode =
			"SDL_VIDEO_X11_XVIDMODE";
		public const string SdlHintVideoX11Xinerama =
			"SDL_VIDEO_X11_XINERAMA";
		public const string SdlHintVideoX11Xrandr =
			"SDL_VIDEO_X11_XRANDR";
		public const string SdlHintGrabKeyboard =
			"SDL_GRAB_KEYBOARD";
		public const string SdlHintVideoMinimizeOnFocusLoss =
			"SDL_VIDEO_MINIMIZE_ON_FOCUS_LOSS";
		public const string SdlHintIdleTimerDisabled =
			"SDL_IOS_IDLE_TIMER_DISABLED";
		public const string SdlHintOrientations =
			"SDL_IOS_ORIENTATIONS";
		public const string SdlHintXinputEnabled =
			"SDL_XINPUT_ENABLED";
		public const string SdlHintGamecontrollerconfig =
			"SDL_GAMECONTROLLERCONFIG";
		public const string SdlHintJoystickAllowBackgroundEvents =
			"SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS";
		public const string SdlHintAllowTopmost =
			"SDL_ALLOW_TOPMOST";
		public const string SdlHintTimerResolution =
			"SDL_TIMER_RESOLUTION";
		public const string SdlHintRenderScaleQuality =
			"SDL_RENDER_SCALE_QUALITY";

		/* Only available in SDL 2.0.1 or higher. */
		public const string SdlHintVideoHighdpiDisabled =
			"SDL_VIDEO_HIGHDPI_DISABLED";

		/* Only available in SDL 2.0.2 or higher. */
		public const string SdlHintCtrlClickEmulateRightClick =
			"SDL_CTRL_CLICK_EMULATE_RIGHT_CLICK";
		public const string SdlHintVideoWinD3Dcompiler =
			"SDL_VIDEO_WIN_D3DCOMPILER";
		public const string SdlHintMouseRelativeModeWarp =
			"SDL_MOUSE_RELATIVE_MODE_WARP";
		public const string SdlHintVideoWindowSharePixelFormat =
			"SDL_VIDEO_WINDOW_SHARE_PIXEL_FORMAT";
		public const string SdlHintVideoAllowScreensaver =
			"SDL_VIDEO_ALLOW_SCREENSAVER";
		public const string SdlHintAccelerometerAsJoystick =
			"SDL_ACCELEROMETER_AS_JOYSTICK";
		public const string SdlHintVideoMacFullscreenSpaces =
			"SDL_VIDEO_MAC_FULLSCREEN_SPACES";

		/* Only available in SDL 2.0.3 or higher. */
		public const string SdlHintWinrtPrivacyPolicyUrl =
			"SDL_WINRT_PRIVACY_POLICY_URL";
		public const string SdlHintWinrtPrivacyPolicyLabel =
			"SDL_WINRT_PRIVACY_POLICY_LABEL";
		public const string SdlHintWinrtHandleBackButton =
			"SDL_WINRT_HANDLE_BACK_BUTTON";

		/* Only available in SDL 2.0.4 or higher. */
		public const string SdlHintNoSignalHandlers =
			"SDL_NO_SIGNAL_HANDLERS";
		public const string SdlHintImeInternalEditing =
			"SDL_IME_INTERNAL_EDITING";
		public const string SdlHintAndroidSeparateMouseAndTouch =
			"SDL_ANDROID_SEPARATE_MOUSE_AND_TOUCH";
		public const string SdlHintEmscriptenKeyboardElement =
			"SDL_EMSCRIPTEN_KEYBOARD_ELEMENT";
		public const string SdlHintThreadStackSize =
			"SDL_THREAD_STACK_SIZE";
		public const string SdlHintWindowFrameUsableWhileCursorHidden =
			"SDL_WINDOW_FRAME_USABLE_WHILE_CURSOR_HIDDEN";
		public const string SdlHintWindowsEnableMessageloop =
			"SDL_WINDOWS_ENABLE_MESSAGELOOP";
		public const string SdlHintWindowsNoCloseOnAltF4 =
			"SDL_WINDOWS_NO_CLOSE_ON_ALT_F4";
		public const string SdlHintXinputUseOldJoystickMapping =
			"SDL_XINPUT_USE_OLD_JOYSTICK_MAPPING";
		public const string SdlHintMacBackgroundApp =
			"SDL_MAC_BACKGROUND_APP";
		public const string SdlHintVideoX11NetWmPing =
			"SDL_VIDEO_X11_NET_WM_PING";
		public const string SdlHintAndroidApkExpansionMainFileVersion =
			"SDL_ANDROID_APK_EXPANSION_MAIN_FILE_VERSION";
		public const string SdlHintAndroidApkExpansionPatchFileVersion =
			"SDL_ANDROID_APK_EXPANSION_PATCH_FILE_VERSION";

		/* Only available in 2.0.5 or higher. */
		public const string SdlHintMouseFocusClickthrough =
			"SDL_MOUSE_FOCUS_CLICKTHROUGH";
		public const string SdlHintBmpSaveLegacyFormat =
			"SDL_BMP_SAVE_LEGACY_FORMAT";
		public const string SdlHintWindowsDisableThreadNaming =
			"SDL_WINDOWS_DISABLE_THREAD_NAMING";
		public const string SdlHintAppleTvRemoteAllowRotation =
			"SDL_APPLE_TV_REMOTE_ALLOW_ROTATION";

		/* Only available in 2.0.6 or higher. */
		public const string SdlHintAudioResamplingMode =
			"SDL_AUDIO_RESAMPLING_MODE";
		public const string SdlHintRenderLogicalSizeMode =
			"SDL_RENDER_LOGICAL_SIZE_MODE";
		public const string SdlHintMouseNormalSpeedScale =
			"SDL_MOUSE_NORMAL_SPEED_SCALE";
		public const string SdlHintMouseRelativeSpeedScale =
			"SDL_MOUSE_RELATIVE_SPEED_SCALE";
		public const string SdlHintTouchMouseEvents =
			"SDL_TOUCH_MOUSE_EVENTS";
		public const string SdlHintWindowsIntresourceIcon =
			"SDL_WINDOWS_INTRESOURCE_ICON";
		public const string SdlHintWindowsIntresourceIconSmall =
			"SDL_WINDOWS_INTRESOURCE_ICON_SMALL";

		/* Only available in 2.0.8 or higher. */
		public const string SdlHintIosHideHomeIndicator =
			"SDL_IOS_HIDE_HOME_INDICATOR";
		public const string SdlHintTvRemoteAsJoystick =
			"SDL_TV_REMOTE_AS_JOYSTICK";
		public const string SdlVideoX11NetWmBypassCompositor =
			"SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR";

		/* Only available in 2.0.9 or higher. */
		public const string SdlHintMouseDoubleClickTime =
			"SDL_MOUSE_DOUBLE_CLICK_TIME";
		public const string SdlHintMouseDoubleClickRadius =
			"SDL_MOUSE_DOUBLE_CLICK_RADIUS";
		public const string SdlHintJoystickHidapi =
			"SDL_JOYSTICK_HIDAPI";
		public const string SdlHintJoystickHidapiPs4 =
			"SDL_JOYSTICK_HIDAPI_PS4";
		public const string SdlHintJoystickHidapiPs4Rumble =
			"SDL_JOYSTICK_HIDAPI_PS4_RUMBLE";
		public const string SdlHintJoystickHidapiSteam =
			"SDL_JOYSTICK_HIDAPI_STEAM";
		public const string SdlHintJoystickHidapiSwitch =
			"SDL_JOYSTICK_HIDAPI_SWITCH";
		public const string SdlHintJoystickHidapiXbox =
			"SDL_JOYSTICK_HIDAPI_XBOX";
		public const string SdlHintEnableSteamControllers =
			"SDL_ENABLE_STEAM_CONTROLLERS";
		public const string SdlHintAndroidTrapBackButton =
			"SDL_ANDROID_TRAP_BACK_BUTTON";

		/* Only available in 2.0.10 or higher. */
		public const string SdlHintMouseTouchEvents =
			"SDL_MOUSE_TOUCH_EVENTS";
		public const string SdlHintGamecontrollerconfigFile =
			"SDL_GAMECONTROLLERCONFIG_FILE";
		public const string SdlHintAndroidBlockOnPause =
			"SDL_ANDROID_BLOCK_ON_PAUSE";
		public const string SdlHintRenderBatching =
			"SDL_RENDER_BATCHING";
		public const string SdlHintEventLogging =
			"SDL_EVENT_LOGGING";
		public const string SdlHintWaveRiffChunkSize =
			"SDL_WAVE_RIFF_CHUNK_SIZE";
		public const string SdlHintWaveTruncation =
			"SDL_WAVE_TRUNCATION";
		public const string SdlHintWaveFactChunk =
			"SDL_WAVE_FACT_CHUNK";

		/* Only available in 2.0.11 or higher. */
		public const string SdlHintVidoX11WindowVisualid =
			"SDL_VIDEO_X11_WINDOW_VISUALID";
		public const string SdlHintGamecontrollerUseButtonLabels =
			"SDL_GAMECONTROLLER_USE_BUTTON_LABELS";
		public const string SdlHintVideoExternalContext =
			"SDL_VIDEO_EXTERNAL_CONTEXT";
		public const string SdlHintJoystickHidapiGamecube =
			"SDL_JOYSTICK_HIDAPI_GAMECUBE";
		public const string SdlHintDisplayUsableBounds =
			"SDL_DISPLAY_USABLE_BOUNDS";
		public const string SdlHintVideoX11ForceEgl =
			"SDL_VIDEO_X11_FORCE_EGL";
		public const string SdlHintGamecontrollertype =
			"SDL_GAMECONTROLLERTYPE";

		/* Only available in 2.0.14 or higher. */
		public const string SdlHintJoystickHidapiCorrelateXinput =
			"SDL_JOYSTICK_HIDAPI_CORRELATE_XINPUT";
		public const string SdlHintJoystickRawinput =
			"SDL_JOYSTICK_RAWINPUT";
		public const string SdlHintAudioDeviceAppName =
			"SDL_AUDIO_DEVICE_APP_NAME";
		public const string SdlHintAudioDeviceStreamName =
			"SDL_AUDIO_DEVICE_STREAM_NAME";
		public const string SdlHintPreferredLocales =
			"SDL_PREFERRED_LOCALES";
		public const string SdlHintThreadPriorityPolicy =
			"SDL_THREAD_PRIORITY_POLICY";
		public const string SdlHintEmscriptenAsyncify =
			"SDL_EMSCRIPTEN_ASYNCIFY";
		public const string SdlHintLinuxJoystickDeadzones =
			"SDL_LINUX_JOYSTICK_DEADZONES";
		public const string SdlHintAndroidBlockOnPausePauseaudio =
			"SDL_ANDROID_BLOCK_ON_PAUSE_PAUSEAUDIO";
		public const string SdlHintJoystickHidapiPs5 =
			"SDL_JOYSTICK_HIDAPI_PS5";
		public const string SdlHintThreadForceRealtimeTimeCritical =
			"SDL_THREAD_FORCE_REALTIME_TIME_CRITICAL";
		public const string SdlHintJoystickThread =
			"SDL_JOYSTICK_THREAD";
		public const string SdlHintAutoUpdateJoysticks =
			"SDL_AUTO_UPDATE_JOYSTICKS";
		public const string SdlHintAutoUpdateSensors =
			"SDL_AUTO_UPDATE_SENSORS";
		public const string SdlHintMouseRelativeScaling =
			"SDL_MOUSE_RELATIVE_SCALING";
		public const string SdlHintJoystickHidapiPs5Rumble =
			"SDL_JOYSTICK_HIDAPI_PS5_RUMBLE";

		public enum SdlHintPriority
		{
			SdlHintDefault,
			SdlHintNormal,
			SdlHintOverride
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_ClearHints();

		[DllImport(NativeLibName, EntryPoint = "SDL_GetHint", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe IntPtr INTERNAL_SDL_GetHint(byte* name);
		public static unsafe string SDL_GetHint(string name)
		{
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];
			return UTF8_ToManaged(
				INTERNAL_SDL_GetHint(
					Utf8Encode(name, utf8Name, utf8NameBufSize)
				)
			);
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_SetHint", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlBool INTERNAL_SDL_SetHint(
			byte* name,
			byte* value
		);
		public static unsafe SdlBool SDL_SetHint(string name, string value)
		{
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];

			int utf8ValueBufSize = Utf8Size(value);
			byte* utf8Value = stackalloc byte[utf8ValueBufSize];

			return INTERNAL_SDL_SetHint(
				Utf8Encode(name, utf8Name, utf8NameBufSize),
				Utf8Encode(value, utf8Value, utf8ValueBufSize)
			);
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_SetHintWithPriority", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlBool INTERNAL_SDL_SetHintWithPriority(
			byte* name,
			byte* value,
			SdlHintPriority priority
		);
		public static unsafe SdlBool SDL_SetHintWithPriority(
			string name,
			string value,
			SdlHintPriority priority
		) {
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];

			int utf8ValueBufSize = Utf8Size(value);
			byte* utf8Value = stackalloc byte[utf8ValueBufSize];

			return INTERNAL_SDL_SetHintWithPriority(
				Utf8Encode(name, utf8Name, utf8NameBufSize),
				Utf8Encode(value, utf8Value, utf8ValueBufSize),
				priority
			);
		}

		/* Only available in 2.0.5 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetHintBoolean", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlBool INTERNAL_SDL_GetHintBoolean(
			byte* name,
			SdlBool defaultValue
		);
		public static unsafe SdlBool SDL_GetHintBoolean(
			string name,
			SdlBool defaultValue
		) {
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];
			return INTERNAL_SDL_GetHintBoolean(
				Utf8Encode(name, utf8Name, utf8NameBufSize),
				defaultValue
			);
		}

		#endregion

		#region SDL_error.h

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_ClearError();

		[DllImport(NativeLibName, EntryPoint = "SDL_GetError", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetError();
		public static string SDL_GetError()
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetError());
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_SetError", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_SetError(byte* fmtAndArglist);
		public static unsafe void SDL_SetError(string fmtAndArglist)
		{
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_SetError(
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* IntPtr refers to a char*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetErrorMsg(IntPtr errstr, int maxlength);

		#endregion

		#region SDL_log.h

		public enum SdlLogCategory
		{
			SdlLogCategoryApplication,
			SdlLogCategoryError,
			SdlLogCategoryAssert,
			SdlLogCategorySystem,
			SdlLogCategoryAudio,
			SdlLogCategoryVideo,
			SdlLogCategoryRender,
			SdlLogCategoryInput,
			SdlLogCategoryTest,

			/* Reserved for future SDL library use */
			SdlLogCategoryReserved1,
			SdlLogCategoryReserved2,
			SdlLogCategoryReserved3,
			SdlLogCategoryReserved4,
			SdlLogCategoryReserved5,
			SdlLogCategoryReserved6,
			SdlLogCategoryReserved7,
			SdlLogCategoryReserved8,
			SdlLogCategoryReserved9,
			SdlLogCategoryReserved10,

			/* Beyond this point is reserved for application use, e.g.
			enum {
				MYAPP_CATEGORY_AWESOME1 = SDL_LOG_CATEGORY_CUSTOM,
				MYAPP_CATEGORY_AWESOME2,
				MYAPP_CATEGORY_AWESOME3,
				...
			};
			*/
			SdlLogCategoryCustom
		}

		public enum SdlLogPriority
		{
			SdlLogPriorityVerbose = 1,
			SdlLogPriorityDebug,
			SdlLogPriorityInfo,
			SdlLogPriorityWarn,
			SdlLogPriorityError,
			SdlLogPriorityCritical,
			SdlNumLogPriorities
		}

		/* userdata refers to a void*, message to a const char* */
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SdlLogOutputFunction(
			IntPtr userdata,
			int category,
			SdlLogPriority priority,
			IntPtr message
		);

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_Log", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_Log(byte* fmtAndArglist);
		public static unsafe void SDL_Log(string fmtAndArglist)
		{
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_Log(
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogVerbose", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogVerbose(
			int category,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogVerbose(
			int category,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogVerbose(
				category,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogDebug", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogDebug(
			int category,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogDebug(
			int category,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogDebug(
				category,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogInfo", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogInfo(
			int category,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogInfo(
			int category,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogInfo(
				category,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogWarn", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogWarn(
			int category,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogWarn(
			int category,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogWarn(
				category,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogError", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogError(
			int category,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogError(
			int category,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogError(
				category,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogCritical", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogCritical(
			int category,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogCritical(
			int category,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogCritical(
				category,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogMessage", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogMessage(
			int category,
			SdlLogPriority priority,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogMessage(
			int category,
			SdlLogPriority priority,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogMessage(
				category,
				priority,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		/* Use string.Format for arglists */
		[DllImport(NativeLibName, EntryPoint = "SDL_LogMessageV", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_LogMessageV(
			int category,
			SdlLogPriority priority,
			byte* fmtAndArglist
		);
		public static unsafe void SDL_LogMessageV(
			int category,
			SdlLogPriority priority,
			string fmtAndArglist
		) {
			int utf8FmtAndArglistBufSize = Utf8Size(fmtAndArglist);
			byte* utf8FmtAndArglist = stackalloc byte[utf8FmtAndArglistBufSize];
			INTERNAL_SDL_LogMessageV(
				category,
				priority,
				Utf8Encode(fmtAndArglist, utf8FmtAndArglist, utf8FmtAndArglistBufSize)
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlLogPriority SDL_LogGetPriority(
			int category
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LogSetPriority(
			int category,
			SdlLogPriority priority
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LogSetAllPriority(
			SdlLogPriority priority
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LogResetPriorities();

		/* userdata refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		private static extern void SDL_LogGetOutputFunction(
			out IntPtr callback,
			out IntPtr userdata
		);
		public static void SDL_LogGetOutputFunction(
			out SdlLogOutputFunction callback,
			out IntPtr userdata
		) {
			IntPtr result = IntPtr.Zero;
			SDL_LogGetOutputFunction(
				out result,
				out userdata
			);
			if (result != IntPtr.Zero)
			{
				callback = (SdlLogOutputFunction) Marshal.GetDelegateForFunctionPointer(
					result,
					typeof(SdlLogOutputFunction)
				);
			}
			else
			{
				callback = null;
			}
		}

		/* userdata refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LogSetOutputFunction(
			SdlLogOutputFunction callback,
			IntPtr userdata
		);

		#endregion

		#region SDL_messagebox.h

		[Flags]
		public enum SdlMessageBoxFlags : uint
		{
			SdlMessageboxError =		0x00000010,
			SdlMessageboxWarning =	0x00000020,
			SdlMessageboxInformation =	0x00000040
		}

		[Flags]
		public enum SdlMessageBoxButtonFlags : uint
		{
			SdlMessageboxButtonReturnkeyDefault = 0x00000001,
			SdlMessageboxButtonEscapekeyDefault = 0x00000002
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct InternalSdlMessageBoxButtonData
		{
			public SdlMessageBoxButtonFlags flags;
			public int buttonid;
			public IntPtr text; /* The UTF-8 button text */
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMessageBoxButtonData
		{
			public SdlMessageBoxButtonFlags flags;
			public int buttonid;
			public string text; /* The UTF-8 button text */
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMessageBoxColor
		{
			public byte r, g, b;
		}

		public enum SdlMessageBoxColorType
		{
			SdlMessageboxColorBackground,
			SdlMessageboxColorText,
			SdlMessageboxColorButtonBorder,
			SdlMessageboxColorButtonBackground,
			SdlMessageboxColorButtonSelected,
			SdlMessageboxColorMax
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMessageBoxColorScheme
		{
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = (int)SdlMessageBoxColorType.SdlMessageboxColorMax)]
				public SdlMessageBoxColor[] colors;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct InternalSdlMessageBoxData
		{
			public SdlMessageBoxFlags flags;
			public IntPtr window;				/* Parent window, can be NULL */
			public IntPtr title;				/* UTF-8 title */
			public IntPtr message;				/* UTF-8 message text */
			public int numbuttons;
			public IntPtr buttons;
			public IntPtr colorScheme;			/* Can be NULL to use system settings */
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMessageBoxData
		{
			public SdlMessageBoxFlags flags;
			public IntPtr window;				/* Parent window, can be NULL */
			public string title;				/* UTF-8 title */
			public string message;				/* UTF-8 message text */
			public int numbuttons;
			public SdlMessageBoxButtonData[] buttons;
			public SdlMessageBoxColorScheme? colorScheme;	/* Can be NULL to use system settings */
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_ShowMessageBox", CallingConvention = CallingConvention.Cdecl)]
		private static extern int INTERNAL_SDL_ShowMessageBox([In()] ref InternalSdlMessageBoxData messageboxdata, out int buttonid);

		/* Ripped from Jameson's LpUtf8StrMarshaler */
		private static IntPtr INTERNAL_AllocUTF8(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return IntPtr.Zero;
			}
			byte[] bytes = Encoding.UTF8.GetBytes(str + '\0');
			IntPtr mem = SDL_malloc((IntPtr) bytes.Length);
			Marshal.Copy(bytes, 0, mem, bytes.Length);
			return mem;
		}

		public static unsafe int SDL_ShowMessageBox([In()] ref SdlMessageBoxData messageboxdata, out int buttonid)
		{
			var data = new InternalSdlMessageBoxData()
			{
				flags = messageboxdata.flags,
				window = messageboxdata.window,
				title = INTERNAL_AllocUTF8(messageboxdata.title),
				message = INTERNAL_AllocUTF8(messageboxdata.message),
				numbuttons = messageboxdata.numbuttons,
			};

			var buttons = new InternalSdlMessageBoxButtonData[messageboxdata.numbuttons];
			for (int i = 0; i < messageboxdata.numbuttons; i++)
			{
				buttons[i] = new InternalSdlMessageBoxButtonData()
				{
					flags = messageboxdata.buttons[i].flags,
					buttonid = messageboxdata.buttons[i].buttonid,
					text = INTERNAL_AllocUTF8(messageboxdata.buttons[i].text),
				};
			}

			if (messageboxdata.colorScheme != null)
			{
				data.colorScheme = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SdlMessageBoxColorScheme)));
				Marshal.StructureToPtr(messageboxdata.colorScheme.Value, data.colorScheme, false);
			}

			int result;
			fixed (InternalSdlMessageBoxButtonData* buttonsPtr = &buttons[0])
			{
				data.buttons = (IntPtr)buttonsPtr;
				result = INTERNAL_SDL_ShowMessageBox(ref data, out buttonid);
			}

			Marshal.FreeHGlobal(data.colorScheme);
			for (int i = 0; i < messageboxdata.numbuttons; i++)
			{
				SDL_free(buttons[i].text);
			}
			SDL_free(data.message);
			SDL_free(data.title);

			return result;
		}

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, EntryPoint = "SDL_ShowSimpleMessageBox", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe int INTERNAL_SDL_ShowSimpleMessageBox(
			SdlMessageBoxFlags flags,
			byte* title,
			byte* message,
			IntPtr window
		);
		public static unsafe int SDL_ShowSimpleMessageBox(
			SdlMessageBoxFlags flags,
			string title,
			string message,
			IntPtr window
		) {
			int utf8TitleBufSize = Utf8Size(title);
			byte* utf8Title = stackalloc byte[utf8TitleBufSize];

			int utf8MessageBufSize = Utf8Size(message);
			byte* utf8Message = stackalloc byte[utf8MessageBufSize];

			return INTERNAL_SDL_ShowSimpleMessageBox(
				flags,
				Utf8Encode(title, utf8Title, utf8TitleBufSize),
				Utf8Encode(message, utf8Message, utf8MessageBufSize),
				window
			);
		}

		#endregion

		#region SDL_version.h, SDL_revision.h

		/* Similar to the headers, this is the version we're expecting to be
		 * running with. You will likely want to check this somewhere in your
		 * program!
		 */
		public const int SdlMajorVersion =	2;
		public const int SdlMinorVersion =	0;
		public const int SdlPatchlevel =	14;

		public static readonly int SdlCompiledversion = SDL_VERSIONNUM(
			SdlMajorVersion,
			SdlMinorVersion,
			SdlPatchlevel
		);

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlVersion
		{
			public byte major;
			public byte minor;
			public byte patch;
		}

		public static void SDL_VERSION(out SdlVersion x)
		{
			x.major = SdlMajorVersion;
			x.minor = SdlMinorVersion;
			x.patch = SdlPatchlevel;
		}

		public static int SDL_VERSIONNUM(int x, int y, int z)
		{
			return (x * 1000) + (y * 100) + z;
		}

		public static bool SDL_VERSION_ATLEAST(int x, int y, int z)
		{
			return (SdlCompiledversion >= SDL_VERSIONNUM(x, y, z));
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetVersion(out SdlVersion ver);

		[DllImport(NativeLibName, EntryPoint = "SDL_GetRevision", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetRevision();
		public static string SDL_GetRevision()
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetRevision());
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetRevisionNumber();

		#endregion

		#region SDL_video.h

		public enum SdlGLattr
		{
			SdlGlRedSize,
			SdlGlGreenSize,
			SdlGlBlueSize,
			SdlGlAlphaSize,
			SdlGlBufferSize,
			SdlGlDoublebuffer,
			SdlGlDepthSize,
			SdlGlStencilSize,
			SdlGlAccumRedSize,
			SdlGlAccumGreenSize,
			SdlGlAccumBlueSize,
			SdlGlAccumAlphaSize,
			SdlGlStereo,
			SdlGlMultisamplebuffers,
			SdlGlMultisamplesamples,
			SdlGlAcceleratedVisual,
			SdlGlRetainedBacking,
			SdlGlContextMajorVersion,
			SdlGlContextMinorVersion,
			SdlGlContextEgl,
			SdlGlContextFlags,
			SdlGlContextProfileMask,
			SdlGlShareWithCurrentContext,
			SdlGlFramebufferSrgbCapable,
			SdlGlContextReleaseBehavior,
			SdlGlContextResetNotification,	/* Requires >= 2.0.6 */
			SdlGlContextNoError,		/* Requires >= 2.0.6 */
		}

		[Flags]
		public enum SdlGLprofile
		{
			SdlGlContextProfileCore				= 0x0001,
			SdlGlContextProfileCompatibility	= 0x0002,
			SdlGlContextProfileEs				= 0x0004
		}

		[Flags]
		public enum SdlGLcontext
		{
			SdlGlContextDebugFlag				= 0x0001,
			SdlGlContextForwardCompatibleFlag	= 0x0002,
			SdlGlContextRobustAccessFlag		= 0x0004,
			SdlGlContextResetIsolationFlag		= 0x0008
		}

		public enum SdlWindowEventId : byte
		{
			SdlWindoweventNone,
			SdlWindoweventShown,
			SdlWindoweventHidden,
			SdlWindoweventExposed,
			SdlWindoweventMoved,
			SdlWindoweventResized,
			SdlWindoweventSizeChanged,
			SdlWindoweventMinimized,
			SdlWindoweventMaximized,
			SdlWindoweventRestored,
			SdlWindoweventEnter,
			SdlWindoweventLeave,
			SdlWindoweventFocusGained,
			SdlWindoweventFocusLost,
			SdlWindoweventClose,
			/* Only available in 2.0.5 or higher. */
			SdlWindoweventTakeFocus,
			SdlWindoweventHitTest
		}

		public enum SdlDisplayEventId : byte
		{
			SdlDisplayeventNone,
			SdlDisplayeventOrientation,
			SdlDisplayeventConnected,	/* Requires >= 2.0.14 */
			SdlDisplayeventDisconnected	/* Requires >= 2.0.14 */
		}

		public enum SdlDisplayOrientation
		{
			SdlOrientationUnknown,
			SdlOrientationLandscape,
			SdlOrientationLandscapeFlipped,
			SdlOrientationPortrait,
			SdlOrientationPortraitFlipped
		}

		[Flags]
		public enum SdlWindowFlags : uint
		{
			SdlWindowFullscreen =		0x00000001,
			SdlWindowOpengl =		0x00000002,
			SdlWindowShown =		0x00000004,
			SdlWindowHidden =		0x00000008,
			SdlWindowBorderless =		0x00000010,
			SdlWindowResizable =		0x00000020,
			SdlWindowMinimized =		0x00000040,
			SdlWindowMaximized =		0x00000080,
			SdlWindowInputGrabbed =	0x00000100,
			SdlWindowInputFocus =	0x00000200,
			SdlWindowMouseFocus =	0x00000400,
			SdlWindowFullscreenDesktop =
				(SdlWindowFullscreen | 0x00001000),
			SdlWindowForeign =		0x00000800,
			SdlWindowAllowHighdpi =	0x00002000,	/* Requires >= 2.0.1 */
			SdlWindowMouseCapture =	0x00004000,	/* Requires >= 2.0.4 */
			SdlWindowAlwaysOnTop =	0x00008000,	/* Requires >= 2.0.5 */
			SdlWindowSkipTaskbar =	0x00010000,	/* Requires >= 2.0.5 */
			SdlWindowUtility =		0x00020000,	/* Requires >= 2.0.5 */
			SdlWindowTooltip =		0x00040000,	/* Requires >= 2.0.5 */
			SdlWindowPopupMenu =		0x00080000,	/* Requires >= 2.0.5 */
			SdlWindowVulkan =		0x10000000,	/* Requires >= 2.0.6 */
			SdlWindowMetal =		0x2000000,	/* Requires >= 2.0.14 */
		}

		/* Only available in 2.0.4 or higher. */
		public enum SdlHitTestResult
		{
			SdlHittestNormal,		/* Region is normal. No special properties. */
			SdlHittestDraggable,		/* Region can drag entire window. */
			SdlHittestResizeTopleft,
			SdlHittestResizeTop,
			SdlHittestResizeTopright,
			SdlHittestResizeRight,
			SdlHittestResizeBottomright,
			SdlHittestResizeBottom,
			SdlHittestResizeBottomleft,
			SdlHittestResizeLeft
		}

		public const int SdlWindowposUndefinedMask =	0x1FFF0000;
		public const int SdlWindowposCenteredMask =	0x2FFF0000;
		public const int SdlWindowposUndefined =	0x1FFF0000;
		public const int SdlWindowposCentered =	0x2FFF0000;

		public static int SDL_WINDOWPOS_UNDEFINED_DISPLAY(int x)
		{
			return (SdlWindowposUndefinedMask | x);
		}

		public static bool SDL_WINDOWPOS_ISUNDEFINED(int x)
		{
			return (x & 0xFFFF0000) == SdlWindowposUndefinedMask;
		}

		public static int SDL_WINDOWPOS_CENTERED_DISPLAY(int x)
		{
			return (SdlWindowposCenteredMask | x);
		}

		public static bool SDL_WINDOWPOS_ISCENTERED(int x)
		{
			return (x & 0xFFFF0000) == SdlWindowposCenteredMask;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlDisplayMode
		{
			public uint format;
			public int w;
			public int h;
			public int refresh_rate;
			public IntPtr driverdata; // void*
		}

		/* win refers to an SDL_Window*, area to a const SDL_Point*, data to a void*.
		 * Only available in 2.0.4 or higher.
		 */
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate SdlHitTestResult SdlHitTest(IntPtr win, IntPtr area, IntPtr data);

		/* IntPtr refers to an SDL_Window* */
		[DllImport(NativeLibName, EntryPoint = "SDL_CreateWindow", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe IntPtr INTERNAL_SDL_CreateWindow(
			byte* title,
			int x,
			int y,
			int w,
			int h,
			SdlWindowFlags flags
		);
		public static unsafe IntPtr SDL_CreateWindow(
			string title,
			int x,
			int y,
			int w,
			int h,
			SdlWindowFlags flags
		) {
			int utf8TitleBufSize = Utf8Size(title);
			byte* utf8Title = stackalloc byte[utf8TitleBufSize];
			return INTERNAL_SDL_CreateWindow(
				Utf8Encode(title, utf8Title, utf8TitleBufSize),
				x, y, w, h,
				flags
			);
		}

		/* window refers to an SDL_Window*, renderer to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_CreateWindowAndRenderer(
			int width,
			int height,
			SdlWindowFlags windowFlags,
			out IntPtr window,
			out IntPtr renderer
		);

		/* data refers to some native window type, IntPtr to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateWindowFrom(IntPtr data);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_DestroyWindow(IntPtr window);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_DisableScreenSaver();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_EnableScreenSaver();

		/* IntPtr refers to an SDL_DisplayMode. Just use closest. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetClosestDisplayMode(
			int displayIndex,
			ref SdlDisplayMode mode,
			out SdlDisplayMode closest
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetCurrentDisplayMode(
			int displayIndex,
			out SdlDisplayMode mode
		);

		[DllImport(NativeLibName, EntryPoint = "SDL_GetCurrentVideoDriver", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetCurrentVideoDriver();
		public static string SDL_GetCurrentVideoDriver()
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetCurrentVideoDriver());
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetDesktopDisplayMode(
			int displayIndex,
			out SdlDisplayMode mode
		);

		[DllImport(NativeLibName, EntryPoint = "SDL_GetDisplayName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetDisplayName(int index);
		public static string SDL_GetDisplayName(int index)
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetDisplayName(index));
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetDisplayBounds(
			int displayIndex,
			out SdlRect rect
		);

		/* Only available in 2.0.4 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetDisplayDPI(
			int displayIndex,
			out float ddpi,
			out float hdpi,
			out float vdpi
		);

		/* Only available in 2.0.9 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlDisplayOrientation SDL_GetDisplayOrientation(
			int displayIndex
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetDisplayMode(
			int displayIndex,
			int modeIndex,
			out SdlDisplayMode mode
		);

		/* Only available in 2.0.5 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetDisplayUsableBounds(
			int displayIndex,
			out SdlRect rect
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumDisplayModes(
			int displayIndex
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumVideoDisplays();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumVideoDrivers();

		[DllImport(NativeLibName, EntryPoint = "SDL_GetVideoDriver", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetVideoDriver(
			int index
		);
		public static string SDL_GetVideoDriver(int index)
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetVideoDriver(index));
		}

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern float SDL_GetWindowBrightness(
			IntPtr window
		);

		/* window refers to an SDL_Window*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowOpacity(
			IntPtr window,
			float opacity
		);

		/* window refers to an SDL_Window*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetWindowOpacity(
			IntPtr window,
			out float outOpacity
		);

		/* modal_window and parent_window refer to an SDL_Window*s
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowModalFor(
			IntPtr modalWindow,
			IntPtr parentWindow
		);

		/* window refers to an SDL_Window*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowInputFocus(IntPtr window);

		/* window refers to an SDL_Window*, IntPtr to a void* */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetWindowData", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe IntPtr INTERNAL_SDL_GetWindowData(
			IntPtr window,
			byte* name
		);
		public static unsafe IntPtr SDL_GetWindowData(
			IntPtr window,
			string name
		) {
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];
			return INTERNAL_SDL_GetWindowData(
				window,
				Utf8Encode(name, utf8Name, utf8NameBufSize)
			);
		}

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetWindowDisplayIndex(
			IntPtr window
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetWindowDisplayMode(
			IntPtr window,
			out SdlDisplayMode mode
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_GetWindowFlags(IntPtr window);

		/* IntPtr refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetWindowFromID(uint id);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetWindowGammaRamp(
			IntPtr window,
			[Out()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeConst = 256)]
				ushort[] red,
			[Out()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeConst = 256)]
				ushort[] green,
			[Out()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeConst = 256)]
				ushort[] blue
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GetWindowGrab(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_GetWindowID(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_GetWindowPixelFormat(
			IntPtr window
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetWindowMaximumSize(
			IntPtr window,
			out int maxW,
			out int maxH
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetWindowMinimumSize(
			IntPtr window,
			out int minW,
			out int minH
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetWindowPosition(
			IntPtr window,
			out int x,
			out int y
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetWindowSize(
			IntPtr window,
			out int w,
			out int h
		);

		/* IntPtr refers to an SDL_Surface*, window to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetWindowSurface(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetWindowTitle", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetWindowTitle(
			IntPtr window
		);
		public static string SDL_GetWindowTitle(IntPtr window)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_GetWindowTitle(window)
			);
		}

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GL_BindTexture(
			IntPtr texture,
			out float texw,
			out float texh
		);

		/* IntPtr and window refer to an SDL_GLContext and SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GL_CreateContext(IntPtr window);

		/* context refers to an SDL_GLContext */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GL_DeleteContext(IntPtr context);

		[DllImport(NativeLibName, EntryPoint = "SDL_GL_LoadLibrary", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe int INTERNAL_SDL_GL_LoadLibrary(byte* path);
		public static unsafe int SDL_GL_LoadLibrary(string path)
		{
			byte* utf8Path = Utf8EncodeHeap(path);
			int result = INTERNAL_SDL_GL_LoadLibrary(
				utf8Path
			);
			Marshal.FreeHGlobal((IntPtr) utf8Path);
			return result;
		}

		/* IntPtr refers to a function pointer, proc to a const char* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GL_GetProcAddress(IntPtr proc);

		/* IntPtr refers to a function pointer */
		public static unsafe IntPtr SDL_GL_GetProcAddress(string proc)
		{
			int utf8ProcBufSize = Utf8Size(proc);
			byte* utf8Proc = stackalloc byte[utf8ProcBufSize];
			return SDL_GL_GetProcAddress(
				(IntPtr) Utf8Encode(proc, utf8Proc, utf8ProcBufSize)
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GL_UnloadLibrary();

		[DllImport(NativeLibName, EntryPoint = "SDL_GL_ExtensionSupported", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlBool INTERNAL_SDL_GL_ExtensionSupported(
			byte* extension
		);
		public static unsafe SdlBool SDL_GL_ExtensionSupported(string extension)
		{
			int utf8ExtensionBufSize = Utf8Size(extension);
			byte* utf8Extension = stackalloc byte[utf8ExtensionBufSize];
			return INTERNAL_SDL_GL_ExtensionSupported(
				Utf8Encode(extension, utf8Extension, utf8ExtensionBufSize)
			);
		}

		/* Only available in SDL 2.0.2 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GL_ResetAttributes();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GL_GetAttribute(
			SdlGLattr attr,
			out int value
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GL_GetSwapInterval();

		/* window and context refer to an SDL_Window* and SDL_GLContext */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GL_MakeCurrent(
			IntPtr window,
			IntPtr context
		);

		/* IntPtr refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GL_GetCurrentWindow();

		/* IntPtr refers to an SDL_Context */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GL_GetCurrentContext();

		/* window refers to an SDL_Window*.
		 * Only available in SDL 2.0.1 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GL_GetDrawableSize(
			IntPtr window,
			out int w,
			out int h
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GL_SetAttribute(
			SdlGLattr attr,
			int value
		);

		public static int SDL_GL_SetAttribute(
			SdlGLattr attr,
			SdlGLprofile profile
		) {
			return SDL_GL_SetAttribute(attr, (int)profile);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GL_SetSwapInterval(int interval);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GL_SwapWindow(IntPtr window);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GL_UnbindTexture(IntPtr texture);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_HideWindow(IntPtr window);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsScreenSaverEnabled();

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_MaximizeWindow(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_MinimizeWindow(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_RaiseWindow(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_RestoreWindow(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowBrightness(
			IntPtr window,
			float brightness
		);

		/* IntPtr and userdata are void*, window is an SDL_Window* */
		[DllImport(NativeLibName, EntryPoint = "SDL_SetWindowData", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe IntPtr INTERNAL_SDL_SetWindowData(
			IntPtr window,
			byte* name,
			IntPtr userdata
		);
		public static unsafe IntPtr SDL_SetWindowData(
			IntPtr window,
			string name,
			IntPtr userdata
		) {
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];
			return INTERNAL_SDL_SetWindowData(
				window,
				Utf8Encode(name, utf8Name, utf8NameBufSize),
				userdata
			);
		}

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowDisplayMode(
			IntPtr window,
			ref SdlDisplayMode mode
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowFullscreen(
			IntPtr window,
			uint flags
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowGammaRamp(
			IntPtr window,
			[In()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeConst = 256)]
				ushort[] red,
			[In()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeConst = 256)]
				ushort[] green,
			[In()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeConst = 256)]
				ushort[] blue
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowGrab(
			IntPtr window,
			SdlBool grabbed
		);

		/* window refers to an SDL_Window*, icon to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowIcon(
			IntPtr window,
			IntPtr icon
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowMaximumSize(
			IntPtr window,
			int maxW,
			int maxH
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowMinimumSize(
			IntPtr window,
			int minW,
			int minH
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowPosition(
			IntPtr window,
			int x,
			int y
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowSize(
			IntPtr window,
			int w,
			int h
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowBordered(
			IntPtr window,
			SdlBool bordered
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetWindowBordersSize(
			IntPtr window,
			out int top,
			out int left,
			out int bottom,
			out int right
		);

		/* window refers to an SDL_Window*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowResizable(
			IntPtr window,
			SdlBool resizable
		);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, EntryPoint = "SDL_SetWindowTitle", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void INTERNAL_SDL_SetWindowTitle(
			IntPtr window,
			byte* title
		);
		public static unsafe void SDL_SetWindowTitle(
			IntPtr window,
			string title
		) {
			int utf8TitleBufSize = Utf8Size(title);
			byte* utf8Title = stackalloc byte[utf8TitleBufSize];
			INTERNAL_SDL_SetWindowTitle(
				window,
				Utf8Encode(title, utf8Title, utf8TitleBufSize)
			);
		}

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_ShowWindow(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UpdateWindowSurface(IntPtr window);

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UpdateWindowSurfaceRects(
			IntPtr window,
			[In] SdlRect[] rects,
			int numrects
		);

		[DllImport(NativeLibName, EntryPoint = "SDL_VideoInit", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe int INTERNAL_SDL_VideoInit(
			byte* driverName
		);
		public static unsafe int SDL_VideoInit(string driverName)
		{
			int utf8DriverNameBufSize = Utf8Size(driverName);
			byte* utf8DriverName = stackalloc byte[utf8DriverNameBufSize];
			return INTERNAL_SDL_VideoInit(
				Utf8Encode(driverName, utf8DriverName, utf8DriverNameBufSize)
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_VideoQuit();

		/* window refers to an SDL_Window*, callback_data to a void*
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetWindowHitTest(
			IntPtr window,
			SdlHitTest callback,
			IntPtr callbackData
		);

		/* IntPtr refers to an SDL_Window*
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetGrabbedWindow();

		#endregion

		#region SDL_blendmode.h

		[Flags]
		public enum SdlBlendMode
		{
			SdlBlendmodeNone =	0x00000000,
			SdlBlendmodeBlend =	0x00000001,
			SdlBlendmodeAdd =	0x00000002,
			SdlBlendmodeMod =	0x00000004,
			SdlBlendmodeMul =	0x00000008,	/* >= 2.0.11 */
			SdlBlendmodeInvalid =	0x7FFFFFFF
		}

		public enum SdlBlendOperation
		{
			SdlBlendoperationAdd		= 0x1,
			SdlBlendoperationSubtract	= 0x2,
			SdlBlendoperationRevSubtract	= 0x3,
			SdlBlendoperationMinimum	= 0x4,
			SdlBlendoperationMaximum	= 0x5
		}

		public enum SdlBlendFactor
		{
			SdlBlendfactorZero			= 0x1,
			SdlBlendfactorOne			= 0x2,
			SdlBlendfactorSrcColor		= 0x3,
			SdlBlendfactorOneMinusSrcColor	= 0x4,
			SdlBlendfactorSrcAlpha		= 0x5,
			SdlBlendfactorOneMinusSrcAlpha	= 0x6,
			SdlBlendfactorDstColor		= 0x7,
			SdlBlendfactorOneMinusDstColor	= 0x8,
			SdlBlendfactorDstAlpha		= 0x9,
			SdlBlendfactorOneMinusDstAlpha	= 0xA
		}

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBlendMode SDL_ComposeCustomBlendMode(
			SdlBlendFactor srcColorFactor,
			SdlBlendFactor dstColorFactor,
			SdlBlendOperation colorOperation,
			SdlBlendFactor srcAlphaFactor,
			SdlBlendFactor dstAlphaFactor,
			SdlBlendOperation alphaOperation
		);

		#endregion

		#region SDL_vulkan.h

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_Vulkan_LoadLibrary", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe int INTERNAL_SDL_Vulkan_LoadLibrary(
			byte* path
		);
		public static unsafe int SDL_Vulkan_LoadLibrary(string path)
		{
			byte* utf8Path = Utf8EncodeHeap(path);
			int result = INTERNAL_SDL_Vulkan_LoadLibrary(
				utf8Path
			);
			Marshal.FreeHGlobal((IntPtr) utf8Path);
			return result;
		}

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_Vulkan_GetVkGetInstanceProcAddr();

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_Vulkan_UnloadLibrary();

		/* window refers to an SDL_Window*, pNames to a const char**.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_Vulkan_GetInstanceExtensions(
			IntPtr window,
			out uint pCount,
			IntPtr[] pNames
		);

		/* window refers to an SDL_Window.
		 * instance refers to a VkInstance.
		 * surface refers to a VkSurfaceKHR.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_Vulkan_CreateSurface(
			IntPtr window,
			IntPtr instance,
			out ulong surface
		);

		/* window refers to an SDL_Window*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_Vulkan_GetDrawableSize(
			IntPtr window,
			out int w,
			out int h
		);

		#endregion

		#region SDL_metal.h

		/* Only available in 2.0.11 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_Metal_CreateView(
			IntPtr window
		);

		/* Only available in 2.0.11 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_Metal_DestroyView(
			IntPtr view
		);

		/* view refers to an SDL_MetalView.
		 * Only available in 2.0.14 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_Metal_GetLayer(
			IntPtr view
		);

		/* window refers to an SDL_Window*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_Metal_GetDrawableSize(
			IntPtr window,
			out int w,
			out int h
		);

		#endregion

		#region SDL_render.h

		[Flags]
		public enum SdlRendererFlags : uint
		{
			SdlRendererSoftware =		0x00000001,
			SdlRendererAccelerated =	0x00000002,
			SdlRendererPresentvsync =	0x00000004,
			SdlRendererTargettexture =	0x00000008
		}

		[Flags]
		public enum SdlRendererFlip
		{
			SdlFlipNone =		0x00000000,
			SdlFlipHorizontal =	0x00000001,
			SdlFlipVertical =	0x00000002
		}

		public enum SdlTextureAccess
		{
			SdlTextureaccessStatic,
			SdlTextureaccessStreaming,
			SdlTextureaccessTarget
		}

		[Flags]
		public enum SdlTextureModulate
		{
			SdlTexturemodulateNone =		0x00000000,
			SdlTexturemodulateHorizontal =	0x00000001,
			SdlTexturemodulateVertical =		0x00000002
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct SdlRendererInfo
		{
			public IntPtr name; // const char*
			public uint flags;
			public uint num_texture_formats;
			public fixed uint texture_formats[16];
			public int max_texture_width;
			public int max_texture_height;
		}

		/* Only available in 2.0.11 or higher. */
		public enum SdlScaleMode
		{
			SdlScaleModeNearest,
			SdlScaleModeLinear,
			SdlScaleModeBest
		}

		/* IntPtr refers to an SDL_Renderer*, window to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateRenderer(
			IntPtr window,
			int index,
			SdlRendererFlags flags
		);

		/* IntPtr refers to an SDL_Renderer*, surface to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateSoftwareRenderer(IntPtr surface);

		/* IntPtr refers to an SDL_Texture*, renderer to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateTexture(
			IntPtr renderer,
			uint format,
			int access,
			int w,
			int h
		);

		/* IntPtr refers to an SDL_Texture*
		 * renderer refers to an SDL_Renderer*
		 * surface refers to an SDL_Surface*
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateTextureFromSurface(
			IntPtr renderer,
			IntPtr surface
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_DestroyRenderer(IntPtr renderer);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_DestroyTexture(IntPtr texture);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumRenderDrivers();

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetRenderDrawBlendMode(
			IntPtr renderer,
			out SdlBlendMode blendMode
		);

		/* texture refers to an SDL_Texture*
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetTextureScaleMode(
			IntPtr texture,
			SdlScaleMode scaleMode
		);

		/* texture refers to an SDL_Texture*
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetTextureScaleMode(
			IntPtr texture,
			out SdlScaleMode scaleMode
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetRenderDrawColor(
			IntPtr renderer,
			out byte r,
			out byte g,
			out byte b,
			out byte a
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetRenderDriverInfo(
			int index,
			out SdlRendererInfo info
		);

		/* IntPtr refers to an SDL_Renderer*, window to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetRenderer(IntPtr window);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetRendererInfo(
			IntPtr renderer,
			out SdlRendererInfo info
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetRendererOutputSize(
			IntPtr renderer,
			out int w,
			out int h
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetTextureAlphaMod(
			IntPtr texture,
			out byte alpha
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetTextureBlendMode(
			IntPtr texture,
			out SdlBlendMode blendMode
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetTextureColorMod(
			IntPtr texture,
			out byte r,
			out byte g,
			out byte b
		);

		/* texture refers to an SDL_Texture*, pixels to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_LockTexture(
			IntPtr texture,
			ref SdlRect rect,
			out IntPtr pixels,
			out int pitch
		);

		/* texture refers to an SDL_Texture*, pixels to a void*.
		 * Internally, this function contains logic to use default values when
		 * the rectangle is passed as NULL.
		 * This overload allows for IntPtr.Zero to be passed for rect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_LockTexture(
			IntPtr texture,
			IntPtr rect,
			out IntPtr pixels,
			out int pitch
		);

		/* texture refers to an SDL_Texture*, surface to an SDL_Surface*
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_LockTextureToSurface(
			IntPtr texture,
			ref SdlRect rect,
			out IntPtr surface
		);

		/* texture refers to an SDL_Texture*, surface to an SDL_Surface*
		 * Internally, this function contains logic to use default values when
		 * the rectangle is passed as NULL.
		 * This overload allows for IntPtr.Zero to be passed for rect.
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_LockTextureToSurface(
			IntPtr texture,
			IntPtr rect,
			out IntPtr surface
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_QueryTexture(
			IntPtr texture,
			out uint format,
			out int access,
			out int w,
			out int h
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderClear(IntPtr renderer);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopy(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			ref SdlRect dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopy(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			ref SdlRect dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopy(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			IntPtr dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both SDL_Rects.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopy(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			IntPtr dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			ref SdlRect dstrect,
			double angle,
			ref SdlPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			ref SdlRect dstrect,
			double angle,
			ref SdlPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			IntPtr dstrect,
			double angle,
			ref SdlPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for center.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			ref SdlRect dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both
		 * srcrect and dstrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			IntPtr dstrect,
			double angle,
			ref SdlPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both
		 * srcrect and center.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			ref SdlRect dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both
		 * dstrect and center.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			IntPtr dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for all
		 * three parameters.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			IntPtr dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawLine(
			IntPtr renderer,
			int x1,
			int y1,
			int x2,
			int y2
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawLines(
			IntPtr renderer,
			[In] SdlPoint[] points,
			int count
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawPoint(
			IntPtr renderer,
			int x,
			int y
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawPoints(
			IntPtr renderer,
			[In] SdlPoint[] points,
			int count
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawRect(
			IntPtr renderer,
			ref SdlRect rect
		);

		/* renderer refers to an SDL_Renderer*, rect to an SDL_Rect*.
		 * This overload allows for IntPtr.Zero (null) to be passed for rect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawRect(
			IntPtr renderer,
			IntPtr rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawRects(
			IntPtr renderer,
			[In] SdlRect[] rects,
			int count
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderFillRect(
			IntPtr renderer,
			ref SdlRect rect
		);

		/* renderer refers to an SDL_Renderer*, rect to an SDL_Rect*.
		 * This overload allows for IntPtr.Zero (null) to be passed for rect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderFillRect(
			IntPtr renderer,
			IntPtr rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderFillRects(
			IntPtr renderer,
			[In] SdlRect[] rects,
			int count
		);

		#region Floating Point Render Functions

		/* This region only available in SDL 2.0.10 or higher. */

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyF(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			ref SdlFRect dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyF(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			ref SdlFRect dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyF(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			IntPtr dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both SDL_Rects.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyF(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			IntPtr dstrect
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			ref SdlFRect dstrect,
			double angle,
			ref SdlFPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyEx(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			ref SdlFRect dstrect,
			double angle,
			ref SdlFPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyExF(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			IntPtr dstrect,
			double angle,
			ref SdlFPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for center.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyExF(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			ref SdlFRect dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both
		 * srcrect and dstrect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyExF(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			IntPtr dstrect,
			double angle,
			ref SdlFPoint center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both
		 * srcrect and center.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyExF(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			ref SdlFRect dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both
		 * dstrect and center.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyExF(
			IntPtr renderer,
			IntPtr texture,
			ref SdlRect srcrect,
			IntPtr dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture*.
		 * Internally, this function contains logic to use default values when
		 * source, destination, and/or center are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for all
		 * three parameters.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderCopyExF(
			IntPtr renderer,
			IntPtr texture,
			IntPtr srcrect,
			IntPtr dstrect,
			double angle,
			IntPtr center,
			SdlRendererFlip flip
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawPointF(
			IntPtr renderer,
			float x,
			float y
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawPointsF(
			IntPtr renderer,
			[In] SdlFPoint[] points,
			int count
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawLineF(
			IntPtr renderer,
			float x1,
			float y1,
			float x2,
			float y2
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawLinesF(
			IntPtr renderer,
			[In] SdlFPoint[] points,
			int count
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawRectF(
			IntPtr renderer,
			ref SdlFRect rect
		);

		/* renderer refers to an SDL_Renderer*, rect to an SDL_Rect*.
		 * This overload allows for IntPtr.Zero (null) to be passed for rect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawRectF(
			IntPtr renderer,
			IntPtr rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderDrawRectsF(
			IntPtr renderer,
			[In] SdlFRect[] rects,
			int count
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderFillRectF(
			IntPtr renderer,
			ref SdlFRect rect
		);

		/* renderer refers to an SDL_Renderer*, rect to an SDL_Rect*.
		 * This overload allows for IntPtr.Zero (null) to be passed for rect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderFillRectF(
			IntPtr renderer,
			IntPtr rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderFillRectsF(
			IntPtr renderer,
			[In] SdlFRect[] rects,
			int count
		);

		#endregion

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_RenderGetClipRect(
			IntPtr renderer,
			out SdlRect rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_RenderGetLogicalSize(
			IntPtr renderer,
			out int w,
			out int h
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_RenderGetScale(
			IntPtr renderer,
			out float scaleX,
			out float scaleY
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderGetViewport(
			IntPtr renderer,
			out SdlRect rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_RenderPresent(IntPtr renderer);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderReadPixels(
			IntPtr renderer,
			ref SdlRect rect,
			uint format,
			IntPtr pixels,
			int pitch
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderSetClipRect(
			IntPtr renderer,
			ref SdlRect rect
		);

		/* renderer refers to an SDL_Renderer*
		 * This overload allows for IntPtr.Zero (null) to be passed for rect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderSetClipRect(
			IntPtr renderer,
			IntPtr rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderSetLogicalSize(
			IntPtr renderer,
			int w,
			int h
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderSetScale(
			IntPtr renderer,
			float scaleX,
			float scaleY
		);

		/* renderer refers to an SDL_Renderer*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderSetIntegerScale(
			IntPtr renderer,
			SdlBool enable
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderSetViewport(
			IntPtr renderer,
			ref SdlRect rect
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetRenderDrawBlendMode(
			IntPtr renderer,
			SdlBlendMode blendMode
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetRenderDrawColor(
			IntPtr renderer,
			byte r,
			byte g,
			byte b,
			byte a
		);

		/* renderer refers to an SDL_Renderer*, texture to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetRenderTarget(
			IntPtr renderer,
			IntPtr texture
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetTextureAlphaMod(
			IntPtr texture,
			byte alpha
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetTextureBlendMode(
			IntPtr texture,
			SdlBlendMode blendMode
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetTextureColorMod(
			IntPtr texture,
			byte r,
			byte g,
			byte b
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_UnlockTexture(IntPtr texture);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UpdateTexture(
			IntPtr texture,
			ref SdlRect rect,
			IntPtr pixels,
			int pitch
		);

		/* texture refers to an SDL_Texture* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UpdateTexture(
			IntPtr texture,
			IntPtr rect,
			IntPtr pixels,
			int pitch
		);

		/* texture refers to an SDL_Texture*
		 * Only available in 2.0.1 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UpdateYUVTexture(
			IntPtr texture,
			ref SdlRect rect,
			IntPtr yPlane,
			int yPitch,
			IntPtr uPlane,
			int uPitch,
			IntPtr vPlane,
			int vPitch
		);

		/* renderer refers to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_RenderTargetSupported(
			IntPtr renderer
		);

		/* IntPtr refers to an SDL_Texture*, renderer to an SDL_Renderer* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetRenderTarget(IntPtr renderer);

		/* renderer refers to an SDL_Renderer*
		 * Only available in 2.0.8 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_RenderGetMetalLayer(
			IntPtr renderer
		);

		/* renderer refers to an SDL_Renderer*
		 * Only available in 2.0.8 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_RenderGetMetalCommandEncoder(
			IntPtr renderer
		);

		/* renderer refers to an SDL_Renderer*
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_RenderIsClipEnabled(IntPtr renderer);

		/* renderer refers to an SDL_Renderer*
		 * Only available in 2.0.10 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_RenderFlush(IntPtr renderer);

		#endregion

		#region SDL_pixels.h

		public static uint SDL_DEFINE_PIXELFOURCC(byte a, byte b, byte c, byte d)
		{
			return SDL_FOURCC(a, b, c, d);
		}

		public static uint SDL_DEFINE_PIXELFORMAT(
			SdlPixelType type,
			uint order,
			SdlPackedLayout layout,
			byte bits,
			byte bytes
		) {
			return (uint) (
				(1 << 28) |
				(((byte) type) << 24) |
				(((byte) order) << 20) |
				(((byte) layout) << 16) |
				(bits << 8) |
				(bytes)
			);
		}

		public static byte SDL_PIXELFLAG(uint x)
		{
			return (byte) ((x >> 28) & 0x0F);
		}

		public static byte SDL_PIXELTYPE(uint x)
		{
			return (byte) ((x >> 24) & 0x0F);
		}

		public static byte SDL_PIXELORDER(uint x)
		{
			return (byte) ((x >> 20) & 0x0F);
		}

		public static byte SDL_PIXELLAYOUT(uint x)
		{
			return (byte) ((x >> 16) & 0x0F);
		}

		public static byte SDL_BITSPERPIXEL(uint x)
		{
			return (byte) ((x >> 8) & 0xFF);
		}

		public static byte SDL_BYTESPERPIXEL(uint x)
		{
			if (SDL_ISPIXELFORMAT_FOURCC(x))
			{
				if (	(x == SdlPixelformatYuy2) ||
						(x == SdlPixelformatUyvy) ||
						(x == SdlPixelformatYvyu)	)
				{
					return 2;
				}
				return 1;
			}
			return (byte) (x & 0xFF);
		}

		public static bool SDL_ISPIXELFORMAT_INDEXED(uint format)
		{
			if (SDL_ISPIXELFORMAT_FOURCC(format))
			{
				return false;
			}
			SdlPixelType pType =
				(SdlPixelType) SDL_PIXELTYPE(format);
			return (
				pType == SdlPixelType.SdlPixeltypeIndex1 ||
				pType == SdlPixelType.SdlPixeltypeIndex4 ||
				pType == SdlPixelType.SdlPixeltypeIndex8
			);
		}

		public static bool SDL_ISPIXELFORMAT_PACKED(uint format)
		{
			if (SDL_ISPIXELFORMAT_FOURCC(format))
			{
				return false;
			}
			SdlPixelType pType =
				(SdlPixelType) SDL_PIXELTYPE(format);
			return (
				pType == SdlPixelType.SdlPixeltypePacked8 ||
				pType == SdlPixelType.SdlPixeltypePacked16 ||
				pType == SdlPixelType.SdlPixeltypePacked32
			);
		}

		public static bool SDL_ISPIXELFORMAT_ARRAY(uint format)
		{
			if (SDL_ISPIXELFORMAT_FOURCC(format))
			{
				return false;
			}
			SdlPixelType pType =
				(SdlPixelType) SDL_PIXELTYPE(format);
			return (
				pType == SdlPixelType.SdlPixeltypeArrayu8 ||
				pType == SdlPixelType.SdlPixeltypeArrayu16 ||
				pType == SdlPixelType.SdlPixeltypeArrayu32 ||
				pType == SdlPixelType.SdlPixeltypeArrayf16 ||
				pType == SdlPixelType.SdlPixeltypeArrayf32
			);
		}

		public static bool SDL_ISPIXELFORMAT_ALPHA(uint format)
		{
			if (SDL_ISPIXELFORMAT_PACKED(format))
			{
				SdlPackedOrder pOrder =
					(SdlPackedOrder) SDL_PIXELORDER(format);
				return (
					pOrder == SdlPackedOrder.SdlPackedorderArgb ||
					pOrder == SdlPackedOrder.SdlPackedorderRgba ||
					pOrder == SdlPackedOrder.SdlPackedorderAbgr ||
					pOrder == SdlPackedOrder.SdlPackedorderBgra
				);
			}
			else if (SDL_ISPIXELFORMAT_ARRAY(format))
			{
				SdlArrayOrder aOrder =
					(SdlArrayOrder) SDL_PIXELORDER(format);
				return (
					aOrder == SdlArrayOrder.SdlArrayorderArgb ||
					aOrder == SdlArrayOrder.SdlArrayorderRgba ||
					aOrder == SdlArrayOrder.SdlArrayorderAbgr ||
					aOrder == SdlArrayOrder.SdlArrayorderBgra
				);
			}
			return false;
		}

		public static bool SDL_ISPIXELFORMAT_FOURCC(uint format)
		{
			return (format == 0) && (SDL_PIXELFLAG(format) != 1);
		}

		public enum SdlPixelType
		{
			SdlPixeltypeUnknown,
			SdlPixeltypeIndex1,
			SdlPixeltypeIndex4,
			SdlPixeltypeIndex8,
			SdlPixeltypePacked8,
			SdlPixeltypePacked16,
			SdlPixeltypePacked32,
			SdlPixeltypeArrayu8,
			SdlPixeltypeArrayu16,
			SdlPixeltypeArrayu32,
			SdlPixeltypeArrayf16,
			SdlPixeltypeArrayf32
		}

		public enum SdlBitmapOrder
		{
			SdlBitmaporderNone,
			SdlBitmaporder4321,
			SdlBitmaporder1234
		}

		public enum SdlPackedOrder
		{
			SdlPackedorderNone,
			SdlPackedorderXrgb,
			SdlPackedorderRgbx,
			SdlPackedorderArgb,
			SdlPackedorderRgba,
			SdlPackedorderXbgr,
			SdlPackedorderBgrx,
			SdlPackedorderAbgr,
			SdlPackedorderBgra
		}

		public enum SdlArrayOrder
		{
			SdlArrayorderNone,
			SdlArrayorderRgb,
			SdlArrayorderRgba,
			SdlArrayorderArgb,
			SdlArrayorderBgr,
			SdlArrayorderBgra,
			SdlArrayorderAbgr
		}

		public enum SdlPackedLayout
		{
			SdlPackedlayoutNone,
			SdlPackedlayout332,
			SdlPackedlayout4444,
			SdlPackedlayout1555,
			SdlPackedlayout5551,
			SdlPackedlayout565,
			SdlPackedlayout8888,
			SdlPackedlayout2101010,
			SdlPackedlayout1010102
		}

		public static readonly uint SdlPixelformatUnknown = 0;
		public static readonly uint SdlPixelformatIndex1Lsb =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeIndex1,
				(uint) SdlBitmapOrder.SdlBitmaporder4321,
				0,
				1, 0
			);
		public static readonly uint SdlPixelformatIndex1Msb =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeIndex1,
				(uint) SdlBitmapOrder.SdlBitmaporder1234,
				0,
				1, 0
			);
		public static readonly uint SdlPixelformatIndex4Lsb =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeIndex4,
				(uint) SdlBitmapOrder.SdlBitmaporder4321,
				0,
				4, 0
			);
		public static readonly uint SdlPixelformatIndex4Msb =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeIndex4,
				(uint) SdlBitmapOrder.SdlBitmaporder1234,
				0,
				4, 0
			);
		public static readonly uint SdlPixelformatIndex8 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeIndex8,
				0,
				0,
				8, 1
			);
		public static readonly uint SdlPixelformatRgb332 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked8,
				(uint) SdlPackedOrder.SdlPackedorderXrgb,
				SdlPackedLayout.SdlPackedlayout332,
				8, 1
			);
		public static readonly uint SdlPixelformatXrgb444 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderXrgb,
				SdlPackedLayout.SdlPackedlayout4444,
				12, 2
			);
		public static readonly uint SdlPixelformatRgb444 =
			SdlPixelformatXrgb444;
		public static readonly uint SdlPixelformatXbgr444 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderXbgr,
				SdlPackedLayout.SdlPackedlayout4444,
				12, 2
			);
		public static readonly uint SdlPixelformatBgr444 =
			SdlPixelformatXbgr444;
		public static readonly uint SdlPixelformatXrgb1555 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderXrgb,
				SdlPackedLayout.SdlPackedlayout1555,
				15, 2
			);
		public static readonly uint SdlPixelformatRgb555 =
			SdlPixelformatXrgb1555;
		public static readonly uint SdlPixelformatXbgr1555 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeIndex1,
				(uint) SdlBitmapOrder.SdlBitmaporder4321,
				SdlPackedLayout.SdlPackedlayout1555,
				15, 2
			);
		public static readonly uint SdlPixelformatBgr555 =
			SdlPixelformatXbgr1555;
		public static readonly uint SdlPixelformatArgb4444 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderArgb,
				SdlPackedLayout.SdlPackedlayout4444,
				16, 2
			);
		public static readonly uint SdlPixelformatRgba4444 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderRgba,
				SdlPackedLayout.SdlPackedlayout4444,
				16, 2
			);
		public static readonly uint SdlPixelformatAbgr4444 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderAbgr,
				SdlPackedLayout.SdlPackedlayout4444,
				16, 2
			);
		public static readonly uint SdlPixelformatBgra4444 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderBgra,
				SdlPackedLayout.SdlPackedlayout4444,
				16, 2
			);
		public static readonly uint SdlPixelformatArgb1555 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderArgb,
				SdlPackedLayout.SdlPackedlayout1555,
				16, 2
			);
		public static readonly uint SdlPixelformatRgba5551 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderRgba,
				SdlPackedLayout.SdlPackedlayout5551,
				16, 2
			);
		public static readonly uint SdlPixelformatAbgr1555 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderAbgr,
				SdlPackedLayout.SdlPackedlayout1555,
				16, 2
			);
		public static readonly uint SdlPixelformatBgra5551 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderBgra,
				SdlPackedLayout.SdlPackedlayout5551,
				16, 2
			);
		public static readonly uint SdlPixelformatRgb565 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderXrgb,
				SdlPackedLayout.SdlPackedlayout565,
				16, 2
			);
		public static readonly uint SdlPixelformatBgr565 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked16,
				(uint) SdlPackedOrder.SdlPackedorderXbgr,
				SdlPackedLayout.SdlPackedlayout565,
				16, 2
			);
		public static readonly uint SdlPixelformatRgb24 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeArrayu8,
				(uint) SdlArrayOrder.SdlArrayorderRgb,
				0,
				24, 3
			);
		public static readonly uint SdlPixelformatBgr24 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypeArrayu8,
				(uint) SdlArrayOrder.SdlArrayorderBgr,
				0,
				24, 3
			);
		public static readonly uint SdlPixelformatXrgb888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderXrgb,
				SdlPackedLayout.SdlPackedlayout8888,
				24, 4
			);
		public static readonly uint SdlPixelformatRgb888 =
			SdlPixelformatXrgb888;
		public static readonly uint SdlPixelformatRgbx8888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderRgbx,
				SdlPackedLayout.SdlPackedlayout8888,
				24, 4
			);
		public static readonly uint SdlPixelformatXbgr888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderXbgr,
				SdlPackedLayout.SdlPackedlayout8888,
				24, 4
			);
		public static readonly uint SdlPixelformatBgr888 =
			SdlPixelformatXbgr888;
		public static readonly uint SdlPixelformatBgrx8888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderBgrx,
				SdlPackedLayout.SdlPackedlayout8888,
				24, 4
			);
		public static readonly uint SdlPixelformatArgb8888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderArgb,
				SdlPackedLayout.SdlPackedlayout8888,
				32, 4
			);
		public static readonly uint SdlPixelformatRgba8888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderRgba,
				SdlPackedLayout.SdlPackedlayout8888,
				32, 4
			);
		public static readonly uint SdlPixelformatAbgr8888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderAbgr,
				SdlPackedLayout.SdlPackedlayout8888,
				32, 4
			);
		public static readonly uint SdlPixelformatBgra8888 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderBgra,
				SdlPackedLayout.SdlPackedlayout8888,
				32, 4
			);
		public static readonly uint SdlPixelformatArgb2101010 =
			SDL_DEFINE_PIXELFORMAT(
				SdlPixelType.SdlPixeltypePacked32,
				(uint) SdlPackedOrder.SdlPackedorderArgb,
				SdlPackedLayout.SdlPackedlayout2101010,
				32, 4
			);
		public static readonly uint SdlPixelformatYv12 =
			SDL_DEFINE_PIXELFOURCC(
				(byte) 'Y', (byte) 'V', (byte) '1', (byte) '2'
			);
		public static readonly uint SdlPixelformatIyuv =
			SDL_DEFINE_PIXELFOURCC(
				(byte) 'I', (byte) 'Y', (byte) 'U', (byte) 'V'
			);
		public static readonly uint SdlPixelformatYuy2 =
			SDL_DEFINE_PIXELFOURCC(
				(byte) 'Y', (byte) 'U', (byte) 'Y', (byte) '2'
			);
		public static readonly uint SdlPixelformatUyvy =
			SDL_DEFINE_PIXELFOURCC(
				(byte) 'U', (byte) 'Y', (byte) 'V', (byte) 'Y'
			);
		public static readonly uint SdlPixelformatYvyu =
			SDL_DEFINE_PIXELFOURCC(
				(byte) 'Y', (byte) 'V', (byte) 'Y', (byte) 'U'
			);

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlColor
		{
			public byte r;
			public byte g;
			public byte b;
			public byte a;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlPalette
		{
			public int ncolors;
			public IntPtr colors;
			public int version;
			public int refcount;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlPixelFormat
		{
			public uint format;
			public IntPtr palette; // SDL_Palette*
			public byte BitsPerPixel;
			public byte BytesPerPixel;
			public uint Rmask;
			public uint Gmask;
			public uint Bmask;
			public uint Amask;
			public byte Rloss;
			public byte Gloss;
			public byte Bloss;
			public byte Aloss;
			public byte Rshift;
			public byte Gshift;
			public byte Bshift;
			public byte Ashift;
			public int refcount;
			public IntPtr next; // SDL_PixelFormat*
		}

		/* IntPtr refers to an SDL_PixelFormat* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_AllocFormat(uint pixelFormat);

		/* IntPtr refers to an SDL_Palette* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_AllocPalette(int ncolors);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_CalculateGammaRamp(
			float gamma,
			[Out()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeConst = 256)]
				ushort[] ramp
		);

		/* format refers to an SDL_PixelFormat* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeFormat(IntPtr format);

		/* palette refers to an SDL_Palette* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreePalette(IntPtr palette);

		[DllImport(NativeLibName, EntryPoint = "SDL_GetPixelFormatName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetPixelFormatName(
			uint format
		);
		public static string SDL_GetPixelFormatName(uint format)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_GetPixelFormatName(format)
			);
		}

		/* format refers to an SDL_PixelFormat* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetRGB(
			uint pixel,
			IntPtr format,
			out byte r,
			out byte g,
			out byte b
		);

		/* format refers to an SDL_PixelFormat* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetRGBA(
			uint pixel,
			IntPtr format,
			out byte r,
			out byte g,
			out byte b,
			out byte a
		);

		/* format refers to an SDL_PixelFormat* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_MapRGB(
			IntPtr format,
			byte r,
			byte g,
			byte b
		);

		/* format refers to an SDL_PixelFormat* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_MapRGBA(
			IntPtr format,
			byte r,
			byte g,
			byte b,
			byte a
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_MasksToPixelFormatEnum(
			int bpp,
			uint rmask,
			uint gmask,
			uint bmask,
			uint amask
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_PixelFormatEnumToMasks(
			uint format,
			out int bpp,
			out uint rmask,
			out uint gmask,
			out uint bmask,
			out uint amask
		);

		/* palette refers to an SDL_Palette* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetPaletteColors(
			IntPtr palette,
			[In] SdlColor[] colors,
			int firstcolor,
			int ncolors
		);

		/* format and palette refer to an SDL_PixelFormat* and SDL_Palette* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetPixelFormatPalette(
			IntPtr format,
			IntPtr palette
		);

		#endregion

		#region SDL_rect.h

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlPoint
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlRect
		{
			public int x;
			public int y;
			public int w;
			public int h;
		}

		/* Only available in 2.0.10 or higher. */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlFPoint
		{
			public float x;
			public float y;
		}

		/* Only available in 2.0.10 or higher. */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlFRect
		{
			public float x;
			public float y;
			public float w;
			public float h;
		}

		/* Only available in 2.0.4 or higher. */
		public static SdlBool SDL_PointInRect(ref SdlPoint p, ref SdlRect r)
		{
			return (	(p.x >= r.x) &&
					(p.x < (r.x + r.w)) &&
					(p.y >= r.y) &&
					(p.y < (r.y + r.h))	) ?
				SdlBool.SdlTrue :
				SdlBool.SdlFalse;
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_EnclosePoints(
			[In] SdlPoint[] points,
			int count,
			ref SdlRect clip,
			out SdlRect result
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasIntersection(
			ref SdlRect a,
			ref SdlRect b
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IntersectRect(
			ref SdlRect a,
			ref SdlRect b,
			out SdlRect result
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IntersectRectAndLine(
			ref SdlRect rect,
			ref int x1,
			ref int y1,
			ref int x2,
			ref int y2
		);

		public static SdlBool SDL_RectEmpty(ref SdlRect r)
		{
			return ((r.w <= 0) || (r.h <= 0)) ?
				SdlBool.SdlTrue :
				SdlBool.SdlFalse;
		}

		public static SdlBool SDL_RectEquals(
			ref SdlRect a,
			ref SdlRect b
		) {
			return (	(a.x == b.x) &&
					(a.y == b.y) &&
					(a.w == b.w) &&
					(a.h == b.h)	) ?
				SdlBool.SdlTrue :
				SdlBool.SdlFalse;
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_UnionRect(
			ref SdlRect a,
			ref SdlRect b,
			out SdlRect result
		);

		#endregion

		#region SDL_surface.h

		public const uint SdlSwsurface =	0x00000000;
		public const uint SdlPrealloc =	0x00000001;
		public const uint SdlRleaccel =	0x00000002;
		public const uint SdlDontfree =	0x00000004;

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlSurface
		{
			public uint flags;
			public IntPtr format; // SDL_PixelFormat*
			public int w;
			public int h;
			public int pitch;
			public IntPtr pixels; // void*
			public IntPtr userdata; // void*
			public int locked;
			public IntPtr list_blitmap; // void*
			public SdlRect clip_rect;
			public IntPtr map; // SDL_BlitMap*
			public int refcount;
		}

		/* surface refers to an SDL_Surface* */
		public static bool SDL_MUSTLOCK(IntPtr surface)
		{
			SdlSurface sur;
			sur = (SdlSurface) Marshal.PtrToStructure(
				surface,
				typeof(SdlSurface)
			);
			return (sur.flags & SdlRleaccel) != 0;
		}

		/* src and dst refer to an SDL_Surface* */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlit", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitSurface(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* src and dst refer to an SDL_Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlit", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitSurface(
			IntPtr src,
			IntPtr srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* src and dst refer to an SDL_Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlit", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitSurface(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			IntPtr dstrect
		);

		/* src and dst refer to an SDL_Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both SDL_Rects.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlit", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitSurface(
			IntPtr src,
			IntPtr srcrect,
			IntPtr dst,
			IntPtr dstrect
		);

		/* src and dst refer to an SDL_Surface* */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlitScaled", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitScaled(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* src and dst refer to an SDL_Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for srcrect.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlitScaled", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitScaled(
			IntPtr src,
			IntPtr srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* src and dst refer to an SDL_Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for dstrect.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlitScaled", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitScaled(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			IntPtr dstrect
		);

		/* src and dst refer to an SDL_Surface*
		 * Internally, this function contains logic to use default values when
		 * source and destination rectangles are passed as NULL.
		 * This overload allows for IntPtr.Zero (null) to be passed for both SDL_Rects.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_UpperBlitScaled", CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_BlitScaled(
			IntPtr src,
			IntPtr srcrect,
			IntPtr dst,
			IntPtr dstrect
		);

		/* src and dst are void* pointers */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_ConvertPixels(
			int width,
			int height,
			uint srcFormat,
			IntPtr src,
			int srcPitch,
			uint dstFormat,
			IntPtr dst,
			int dstPitch
		);

		/* IntPtr refers to an SDL_Surface*
		 * src refers to an SDL_Surface*
		 * fmt refers to an SDL_PixelFormat*
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_ConvertSurface(
			IntPtr src,
			IntPtr fmt,
			uint flags
		);

		/* IntPtr refers to an SDL_Surface*, src to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_ConvertSurfaceFormat(
			IntPtr src,
			uint pixelFormat,
			uint flags
		);

		/* IntPtr refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateRGBSurface(
			uint flags,
			int width,
			int height,
			int depth,
			uint rmask,
			uint gmask,
			uint bmask,
			uint amask
		);

		/* IntPtr refers to an SDL_Surface*, pixels to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateRGBSurfaceFrom(
			IntPtr pixels,
			int width,
			int height,
			int depth,
			int pitch,
			uint rmask,
			uint gmask,
			uint bmask,
			uint amask
		);

		/* IntPtr refers to an SDL_Surface*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateRGBSurfaceWithFormat(
			uint flags,
			int width,
			int height,
			int depth,
			uint format
		);

		/* IntPtr refers to an SDL_Surface*, pixels to a void*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateRGBSurfaceWithFormatFrom(
			IntPtr pixels,
			int width,
			int height,
			int depth,
			int pitch,
			uint format
		);

		/* dst refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_FillRect(
			IntPtr dst,
			ref SdlRect rect,
			uint color
		);

		/* dst refers to an SDL_Surface*.
		 * This overload allows passing NULL to rect.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_FillRect(
			IntPtr dst,
			IntPtr rect,
			uint color
		);

		/* dst refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_FillRects(
			IntPtr dst,
			[In] SdlRect[] rects,
			int count,
			uint color
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeSurface(IntPtr surface);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GetClipRect(
			IntPtr surface,
			out SdlRect rect
		);

		/* surface refers to an SDL_Surface*.
		 * Only available in 2.0.9 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasColorKey(IntPtr surface);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetColorKey(
			IntPtr surface,
			out uint key
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetSurfaceAlphaMod(
			IntPtr surface,
			out byte alpha
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetSurfaceBlendMode(
			IntPtr surface,
			out SdlBlendMode blendMode
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetSurfaceColorMod(
			IntPtr surface,
			out byte r,
			out byte g,
			out byte b
		);

		/* These are for SDL_LoadBMP, which is a macro in the SDL headers. */
		/* IntPtr refers to an SDL_Surface* */
		/* THIS IS AN RWops FUNCTION! */
		[DllImport(NativeLibName, EntryPoint = "SDL_LoadBMP_RW", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_LoadBMP_RW(
			IntPtr src,
			int freesrc
		);
		public static IntPtr SDL_LoadBMP(string file)
		{
			IntPtr rwops = SDL_RWFromFile(file, "rb");
			return INTERNAL_SDL_LoadBMP_RW(rwops, 1);
		}

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_LockSurface(IntPtr surface);

		/* src and dst refer to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_LowerBlit(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* src and dst refer to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_LowerBlitScaled(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* These are for SDL_SaveBMP, which is a macro in the SDL headers. */
		/* IntPtr refers to an SDL_Surface* */
		/* THIS IS AN RWops FUNCTION! */
		[DllImport(NativeLibName, EntryPoint = "SDL_SaveBMP_RW", CallingConvention = CallingConvention.Cdecl)]
		private static extern int INTERNAL_SDL_SaveBMP_RW(
			IntPtr surface,
			IntPtr src,
			int freesrc
		);
		public static int SDL_SaveBMP(IntPtr surface, string file)
		{
			IntPtr rwops = SDL_RWFromFile(file, "wb");
			return INTERNAL_SDL_SaveBMP_RW(surface, rwops, 1);
		}

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_SetClipRect(
			IntPtr surface,
			ref SdlRect rect
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetColorKey(
			IntPtr surface,
			int flag,
			uint key
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetSurfaceAlphaMod(
			IntPtr surface,
			byte alpha
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetSurfaceBlendMode(
			IntPtr surface,
			SdlBlendMode blendMode
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetSurfaceColorMod(
			IntPtr surface,
			byte r,
			byte g,
			byte b
		);

		/* surface refers to an SDL_Surface*, palette to an SDL_Palette* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetSurfacePalette(
			IntPtr surface,
			IntPtr palette
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetSurfaceRLE(
			IntPtr surface,
			int flag
		);

		/* surface refers to an SDL_Surface*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasSurfaceRLE(
			IntPtr surface
		);

		/* src and dst refer to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SoftStretch(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* surface refers to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_UnlockSurface(IntPtr surface);

		/* src and dst refer to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UpperBlit(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* src and dst refer to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_UpperBlitScaled(
			IntPtr src,
			ref SdlRect srcrect,
			IntPtr dst,
			ref SdlRect dstrect
		);

		/* surface and IntPtr refer to an SDL_Surface* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_DuplicateSurface(IntPtr surface);

		#endregion

		#region SDL_clipboard.h

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasClipboardText();

		[DllImport(NativeLibName, EntryPoint = "SDL_GetClipboardText", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetClipboardText();
		public static string SDL_GetClipboardText()
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetClipboardText(), true);
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_SetClipboardText", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe int INTERNAL_SDL_SetClipboardText(
			byte* text
		);
		public static unsafe int SDL_SetClipboardText(
			string text
		) {
			byte* utf8Text = Utf8EncodeHeap(text);
			int result = INTERNAL_SDL_SetClipboardText(
				utf8Text
			);
			Marshal.FreeHGlobal((IntPtr) utf8Text);
			return result;
		}

		#endregion

		#region SDL_events.h

		/* General keyboard/mouse state definitions. */
		public const byte SdlPressed =		1;
		public const byte SdlReleased =	0;

		/* Default size is according to SDL2 default. */
		public const int SdlTexteditingeventTextSize = 32;
		public const int SdlTextinputeventTextSize = 32;

		/* The types of events that can be delivered. */
		public enum SdlEventType : uint
		{
			SdlFirstevent =		0,

			/* Application events */
			SdlQuit = 			0x100,

			/* iOS/Android/WinRT app events */
			SdlAppTerminating,
			SdlAppLowmemory,
			SdlAppWillenterbackground,
			SdlAppDidenterbackground,
			SdlAppWillenterforeground,
			SdlAppDidenterforeground,

			/* Only available in SDL 2.0.14 or higher. */
			SdlLocalechanged,

			/* Display events */
			/* Only available in SDL 2.0.9 or higher. */
			SdlDisplayevent =		0x150,

			/* Window events */
			SdlWindowevent = 		0x200,
			SdlSyswmevent,

			/* Keyboard events */
			SdlKeydown = 			0x300,
			SdlKeyup,
			SdlTextediting,
			SdlTextinput,
			SdlKeymapchanged,

			/* Mouse events */
			SdlMousemotion = 		0x400,
			SdlMousebuttondown,
			SdlMousebuttonup,
			SdlMousewheel,

			/* Joystick events */
			SdlJoyaxismotion =		0x600,
			SdlJoyballmotion,
			SdlJoyhatmotion,
			SdlJoybuttondown,
			SdlJoybuttonup,
			SdlJoydeviceadded,
			SdlJoydeviceremoved,

			/* Game controller events */
			SdlControlleraxismotion = 	0x650,
			SdlControllerbuttondown,
			SdlControllerbuttonup,
			SdlControllerdeviceadded,
			SdlControllerdeviceremoved,
			SdlControllerdeviceremapped,
			SdlControllertouchpaddown,	/* Requires >= 2.0.14 */
			SdlControllertouchpadmotion,	/* Requires >= 2.0.14 */
			SdlControllertouchpadup,	/* Requires >= 2.0.14 */
			SdlControllersensorupdate,	/* Requires >= 2.0.14 */

			/* Touch events */
			SdlFingerdown = 		0x700,
			SdlFingerup,
			SdlFingermotion,

			/* Gesture events */
			SdlDollargesture =		0x800,
			SdlDollarrecord,
			SdlMultigesture,

			/* Clipboard events */
			SdlClipboardupdate =		0x900,

			/* Drag and drop events */
			SdlDropfile =			0x1000,
			/* Only available in 2.0.4 or higher. */
			SdlDroptext,
			SdlDropbegin,
			SdlDropcomplete,

			/* Audio hotplug events */
			/* Only available in SDL 2.0.4 or higher. */
			SdlAudiodeviceadded =		0x1100,
			SdlAudiodeviceremoved,

			/* Sensor events */
			/* Only available in SDL 2.0.9 or higher. */
			SdlSensorupdate =		0x1200,

			/* Render events */
			/* Only available in SDL 2.0.2 or higher. */
			SdlRenderTargetsReset =	0x2000,
			/* Only available in SDL 2.0.4 or higher. */
			SdlRenderDeviceReset,

			/* Events SDL_USEREVENT through SDL_LASTEVENT are for
			 * your use, and should be allocated with
			 * SDL_RegisterEvents()
			 */
			SdlUserevent =			0x8000,

			/* The last event, used for bouding arrays. */
			SdlLastevent =			0xFFFF
		}

		/* Only available in 2.0.4 or higher. */
		public enum SdlMouseWheelDirection : uint
		{
			SdlMousewheelNormal,
			SdlMousewheelFlipped
		}

		/* Fields shared by every event */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlGenericEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
		}

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlDisplayEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 display;
			public SdlDisplayEventId displayEvent; // event, lolC#
			private byte padding1;
			private byte padding2;
			private byte padding3;
			public Int32 data1;
		}
#pragma warning restore 0169

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Window state change event data (event.window.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlWindowEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public SdlWindowEventId windowEvent; // event, lolC#
			private byte padding1;
			private byte padding2;
			private byte padding3;
			public Int32 data1;
			public Int32 data2;
		}
#pragma warning restore 0169

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Keyboard button event structure (event.key.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlKeyboardEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public byte state;
			public byte repeat; /* non-zero if this is a repeat */
			private byte padding2;
			private byte padding3;
			public SdlKeysym keysym;
		}
#pragma warning restore 0169

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct SdlTextEditingEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public fixed byte text[SdlTexteditingeventTextSize];
			public Int32 start;
			public Int32 length;
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct SdlTextInputEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public fixed byte text[SdlTextinputeventTextSize];
		}

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Mouse motion event structure (event.motion.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMouseMotionEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public UInt32 which;
			public byte state; /* bitmask of buttons */
			private byte padding1;
			private byte padding2;
			private byte padding3;
			public Int32 x;
			public Int32 y;
			public Int32 xrel;
			public Int32 yrel;
		}
#pragma warning restore 0169

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Mouse button event structure (event.button.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMouseButtonEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public UInt32 which;
			public byte button; /* button id */
			public byte state; /* SDL_PRESSED or SDL_RELEASED */
			public byte clicks; /* 1 for single-click, 2 for double-click, etc. */
			private byte padding1;
			public Int32 x;
			public Int32 y;
		}
#pragma warning restore 0169

		/* Mouse wheel event structure (event.wheel.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMouseWheelEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public UInt32 which;
			public Int32 x; /* amount scrolled horizontally */
			public Int32 y; /* amount scrolled vertically */
			public UInt32 direction; /* Set to one of the SDL_MOUSEWHEEL_* defines */
		}

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Joystick axis motion event structure (event.jaxis.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlJoyAxisEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public byte axis;
			private byte padding1;
			private byte padding2;
			private byte padding3;
			public Int16 axisValue; /* value, lolC# */
			public UInt16 padding4;
		}
#pragma warning restore 0169

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Joystick trackball motion event structure (event.jball.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlJoyBallEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public byte ball;
			private byte padding1;
			private byte padding2;
			private byte padding3;
			public Int16 xrel;
			public Int16 yrel;
		}
#pragma warning restore 0169

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Joystick hat position change event struct (event.jhat.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlJoyHatEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public byte hat; /* index of the hat */
			public byte hatValue; /* value, lolC# */
			private byte padding1;
			private byte padding2;
		}
#pragma warning restore 0169

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Joystick button event structure (event.jbutton.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlJoyButtonEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public byte button;
			public byte state; /* SDL_PRESSED or SDL_RELEASED */
			private byte padding1;
			private byte padding2;
		}
#pragma warning restore 0169

		/* Joystick device event structure (event.jdevice.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlJoyDeviceEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
		}

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Game controller axis motion event (event.caxis.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlControllerAxisEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public byte axis;
			private byte padding1;
			private byte padding2;
			private byte padding3;
			public Int16 axisValue; /* value, lolC# */
			private UInt16 padding4;
		}
#pragma warning restore 0169

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Game controller button event (event.cbutton.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlControllerButtonEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public byte button;
			public byte state;
			private byte padding1;
			private byte padding2;
		}
#pragma warning restore 0169

		/* Game controller device event (event.cdevice.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlControllerDeviceEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which;	/* joystick id for ADDED,
						 * else instance id
						 */
		}

		/* Game controller touchpad event structure (event.ctouchpad.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlControllerTouchpadEvent
		{
			public UInt32 type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public Int32 touchpad;
			public Int32 finger;
			public float x;
			public float y;
			public float pressure;
		}

		/* Game controller sensor event structure (event.csensor.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlControllerSensorEvent
		{
			public UInt32 type;
			public UInt32 timestamp;
			public Int32 which; /* SDL_JoystickID */
			public Int32 sensor;
			public float data1;
			public float data2;
			public float data3;
		}

// Ignore private members used for padding in this struct
#pragma warning disable 0169
		/* Audio device event (event.adevice.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlAudioDeviceEvent
		{
			public UInt32 type;
			public UInt32 timestamp;
			public UInt32 which;
			public byte iscapture;
			private byte padding1;
			private byte padding2;
			private byte padding3;
		}
#pragma warning restore 0169

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlTouchFingerEvent
		{
			public UInt32 type;
			public UInt32 timestamp;
			public Int64 touchId; // SDL_TouchID
			public Int64 fingerId; // SDL_GestureID
			public float x;
			public float y;
			public float dx;
			public float dy;
			public float pressure;
			public uint windowID;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlMultiGestureEvent
		{
			public UInt32 type;
			public UInt32 timestamp;
			public Int64 touchId; // SDL_TouchID
			public float dTheta;
			public float dDist;
			public float x;
			public float y;
			public UInt16 numFingers;
			public UInt16 padding;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlDollarGestureEvent
		{
			public UInt32 type;
			public UInt32 timestamp;
			public Int64 touchId; // SDL_TouchID
			public Int64 gestureId; // SDL_GestureID
			public UInt32 numFingers;
			public float error;
			public float x;
			public float y;
		}

		/* File open request by system (event.drop.*), enabled by
		 * default
		 */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlDropEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;

			/* char* filename, to be freed.
			 * Access the variable EXACTLY ONCE like this:
			 * string s = SDL.UTF8_ToManaged(evt.drop.file, true);
			 */
			public IntPtr file;
			public UInt32 windowID;
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct SdlSensorEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public Int32 which;
			public fixed float data[6];
		}

		/* The "quit requested" event */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlQuitEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
		}

		/* A user defined event (event.user.*) */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlUserEvent
		{
			public UInt32 type;
			public UInt32 timestamp;
			public UInt32 windowID;
			public Int32 code;
			public IntPtr data1; /* user-defined */
			public IntPtr data2; /* user-defined */
		}

		/* A video driver dependent event (event.syswm.*), disabled */
		[StructLayout(LayoutKind.Sequential)]
		public struct SdlSysWmEvent
		{
			public SdlEventType type;
			public UInt32 timestamp;
			public IntPtr msg; /* SDL_SysWMmsg*, system-dependent*/
		}

		/* General event structure */
		// C# doesn't do unions, so we do this ugly thing. */
		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct SdlEvent
		{
			[FieldOffset(0)]
			public SdlEventType type;
			[FieldOffset(0)]
			public SdlEventType typeFSharp;
			[FieldOffset(0)]
			public SdlDisplayEvent display;
			[FieldOffset(0)]
			public SdlWindowEvent window;
			[FieldOffset(0)]
			public SdlKeyboardEvent key;
			[FieldOffset(0)]
			public SdlTextEditingEvent edit;
			[FieldOffset(0)]
			public SdlTextInputEvent text;
			[FieldOffset(0)]
			public SdlMouseMotionEvent motion;
			[FieldOffset(0)]
			public SdlMouseButtonEvent button;
			[FieldOffset(0)]
			public SdlMouseWheelEvent wheel;
			[FieldOffset(0)]
			public SdlJoyAxisEvent jaxis;
			[FieldOffset(0)]
			public SdlJoyBallEvent jball;
			[FieldOffset(0)]
			public SdlJoyHatEvent jhat;
			[FieldOffset(0)]
			public SdlJoyButtonEvent jbutton;
			[FieldOffset(0)]
			public SdlJoyDeviceEvent jdevice;
			[FieldOffset(0)]
			public SdlControllerAxisEvent caxis;
			[FieldOffset(0)]
			public SdlControllerButtonEvent cbutton;
			[FieldOffset(0)]
			public SdlControllerDeviceEvent cdevice;
			[FieldOffset(0)]
			public SdlControllerDeviceEvent ctouchpad;
			[FieldOffset(0)]
			public SdlControllerDeviceEvent csensor;
			[FieldOffset(0)]
			public SdlAudioDeviceEvent adevice;
			[FieldOffset(0)]
			public SdlSensorEvent sensor;
			[FieldOffset(0)]
			public SdlQuitEvent quit;
			[FieldOffset(0)]
			public SdlUserEvent user;
			[FieldOffset(0)]
			public SdlSysWmEvent syswm;
			[FieldOffset(0)]
			public SdlTouchFingerEvent tfinger;
			[FieldOffset(0)]
			public SdlMultiGestureEvent mgesture;
			[FieldOffset(0)]
			public SdlDollarGestureEvent dgesture;
			[FieldOffset(0)]
			public SdlDropEvent drop;
			[FieldOffset(0)]
			private fixed byte padding[56];
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int SdlEventFilter(
			IntPtr userdata, // void*
			IntPtr sdlevent // SDL_Event* event, lolC#
		);

		/* Pump the event loop, getting events from the input devices*/
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_PumpEvents();

		public enum SdlEventaction
		{
			SdlAddevent,
			SdlPeekevent,
			SdlGetevent
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_PeepEvents(
			[Out] SdlEvent[] events,
			int numevents,
			SdlEventaction action,
			SdlEventType minType,
			SdlEventType maxType
		);

		/* Checks to see if certain events are in the event queue */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasEvent(SdlEventType type);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasEvents(
			SdlEventType minType,
			SdlEventType maxType
		);

		/* Clears events from the event queue */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FlushEvent(SdlEventType type);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FlushEvents(
			SdlEventType min,
			SdlEventType max
		);

		/* Polls for currently pending events */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_PollEvent(out SdlEvent @event);

		/* Waits indefinitely for the next event */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_WaitEvent(out SdlEvent @event);

		/* Waits until the specified timeout (in ms) for the next event
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_WaitEventTimeout(out SdlEvent @event, int timeout);

		/* Add an event to the event queue */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_PushEvent(ref SdlEvent @event);

		/* userdata refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetEventFilter(
			SdlEventFilter filter,
			IntPtr userdata
		);

		/* userdata refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		private static extern SdlBool SDL_GetEventFilter(
			out IntPtr filter,
			out IntPtr userdata
		);
		public static SdlBool SDL_GetEventFilter(
			out SdlEventFilter filter,
			out IntPtr userdata
		) {
			IntPtr result = IntPtr.Zero;
			SdlBool retval = SDL_GetEventFilter(out result, out userdata);
			if (result != IntPtr.Zero)
			{
				filter = (SdlEventFilter) Marshal.GetDelegateForFunctionPointer(
					result,
					typeof(SdlEventFilter)
				);
			}
			else
			{
				filter = null;
			}
			return retval;
		}

		/* userdata refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_AddEventWatch(
			SdlEventFilter filter,
			IntPtr userdata
		);

		/* userdata refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_DelEventWatch(
			SdlEventFilter filter,
			IntPtr userdata
		);

		/* userdata refers to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FilterEvents(
			SdlEventFilter filter,
			IntPtr userdata
		);

		/* These are for SDL_EventState() */
		public const int SdlQuery = 		-1;
		public const int SdlIgnore = 		0;
		public const int SdlDisable =		0;
		public const int SdlEnable = 		1;

		/* This function allows you to enable/disable certain events */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern byte SDL_EventState(SdlEventType type, int state);

		/* Get the state of an event */
		public static byte SDL_GetEventState(SdlEventType type)
		{
			return SDL_EventState(type, SdlQuery);
		}

		/* Allocate a set of user-defined events */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_RegisterEvents(int numevents);
		#endregion

		#region SDL_scancode.h

		/* Scancodes based off USB keyboard page (0x07) */
		public enum SdlScancode
		{
			SdlScancodeUnknown = 0,

			SdlScancodeA = 4,
			SdlScancodeB = 5,
			SdlScancodeC = 6,
			SdlScancodeD = 7,
			SdlScancodeE = 8,
			SdlScancodeF = 9,
			SdlScancodeG = 10,
			SdlScancodeH = 11,
			SdlScancodeI = 12,
			SdlScancodeJ = 13,
			SdlScancodeK = 14,
			SdlScancodeL = 15,
			SdlScancodeM = 16,
			SdlScancodeN = 17,
			SdlScancodeO = 18,
			SdlScancodeP = 19,
			SdlScancodeQ = 20,
			SdlScancodeR = 21,
			SdlScancodeS = 22,
			SdlScancodeT = 23,
			SdlScancodeU = 24,
			SdlScancodeV = 25,
			SdlScancodeW = 26,
			SdlScancodeX = 27,
			SdlScancodeY = 28,
			SdlScancodeZ = 29,

			SdlScancode1 = 30,
			SdlScancode2 = 31,
			SdlScancode3 = 32,
			SdlScancode4 = 33,
			SdlScancode5 = 34,
			SdlScancode6 = 35,
			SdlScancode7 = 36,
			SdlScancode8 = 37,
			SdlScancode9 = 38,
			SdlScancode0 = 39,

			SdlScancodeReturn = 40,
			SdlScancodeEscape = 41,
			SdlScancodeBackspace = 42,
			SdlScancodeTab = 43,
			SdlScancodeSpace = 44,

			SdlScancodeMinus = 45,
			SdlScancodeEquals = 46,
			SdlScancodeLeftbracket = 47,
			SdlScancodeRightbracket = 48,
			SdlScancodeBackslash = 49,
			SdlScancodeNonushash = 50,
			SdlScancodeSemicolon = 51,
			SdlScancodeApostrophe = 52,
			SdlScancodeGrave = 53,
			SdlScancodeComma = 54,
			SdlScancodePeriod = 55,
			SdlScancodeSlash = 56,

			SdlScancodeCapslock = 57,

			SdlScancodeF1 = 58,
			SdlScancodeF2 = 59,
			SdlScancodeF3 = 60,
			SdlScancodeF4 = 61,
			SdlScancodeF5 = 62,
			SdlScancodeF6 = 63,
			SdlScancodeF7 = 64,
			SdlScancodeF8 = 65,
			SdlScancodeF9 = 66,
			SdlScancodeF10 = 67,
			SdlScancodeF11 = 68,
			SdlScancodeF12 = 69,

			SdlScancodePrintscreen = 70,
			SdlScancodeScrolllock = 71,
			SdlScancodePause = 72,
			SdlScancodeInsert = 73,
			SdlScancodeHome = 74,
			SdlScancodePageup = 75,
			SdlScancodeDelete = 76,
			SdlScancodeEnd = 77,
			SdlScancodePagedown = 78,
			SdlScancodeRight = 79,
			SdlScancodeLeft = 80,
			SdlScancodeDown = 81,
			SdlScancodeUp = 82,

			SdlScancodeNumlockclear = 83,
			SdlScancodeKpDivide = 84,
			SdlScancodeKpMultiply = 85,
			SdlScancodeKpMinus = 86,
			SdlScancodeKpPlus = 87,
			SdlScancodeKpEnter = 88,
			SdlScancodeKp1 = 89,
			SdlScancodeKp2 = 90,
			SdlScancodeKp3 = 91,
			SdlScancodeKp4 = 92,
			SdlScancodeKp5 = 93,
			SdlScancodeKp6 = 94,
			SdlScancodeKp7 = 95,
			SdlScancodeKp8 = 96,
			SdlScancodeKp9 = 97,
			SdlScancodeKp0 = 98,
			SdlScancodeKpPeriod = 99,

			SdlScancodeNonusbackslash = 100,
			SdlScancodeApplication = 101,
			SdlScancodePower = 102,
			SdlScancodeKpEquals = 103,
			SdlScancodeF13 = 104,
			SdlScancodeF14 = 105,
			SdlScancodeF15 = 106,
			SdlScancodeF16 = 107,
			SdlScancodeF17 = 108,
			SdlScancodeF18 = 109,
			SdlScancodeF19 = 110,
			SdlScancodeF20 = 111,
			SdlScancodeF21 = 112,
			SdlScancodeF22 = 113,
			SdlScancodeF23 = 114,
			SdlScancodeF24 = 115,
			SdlScancodeExecute = 116,
			SdlScancodeHelp = 117,
			SdlScancodeMenu = 118,
			SdlScancodeSelect = 119,
			SdlScancodeStop = 120,
			SdlScancodeAgain = 121,
			SdlScancodeUndo = 122,
			SdlScancodeCut = 123,
			SdlScancodeCopy = 124,
			SdlScancodePaste = 125,
			SdlScancodeFind = 126,
			SdlScancodeMute = 127,
			SdlScancodeVolumeup = 128,
			SdlScancodeVolumedown = 129,
			/* not sure whether there's a reason to enable these */
			/*	SDL_SCANCODE_LOCKINGCAPSLOCK = 130, */
			/*	SDL_SCANCODE_LOCKINGNUMLOCK = 131, */
			/*	SDL_SCANCODE_LOCKINGSCROLLLOCK = 132, */
			SdlScancodeKpComma = 133,
			SdlScancodeKpEqualsas400 = 134,

			SdlScancodeInternational1 = 135,
			SdlScancodeInternational2 = 136,
			SdlScancodeInternational3 = 137,
			SdlScancodeInternational4 = 138,
			SdlScancodeInternational5 = 139,
			SdlScancodeInternational6 = 140,
			SdlScancodeInternational7 = 141,
			SdlScancodeInternational8 = 142,
			SdlScancodeInternational9 = 143,
			SdlScancodeLang1 = 144,
			SdlScancodeLang2 = 145,
			SdlScancodeLang3 = 146,
			SdlScancodeLang4 = 147,
			SdlScancodeLang5 = 148,
			SdlScancodeLang6 = 149,
			SdlScancodeLang7 = 150,
			SdlScancodeLang8 = 151,
			SdlScancodeLang9 = 152,

			SdlScancodeAlterase = 153,
			SdlScancodeSysreq = 154,
			SdlScancodeCancel = 155,
			SdlScancodeClear = 156,
			SdlScancodePrior = 157,
			SdlScancodeReturn2 = 158,
			SdlScancodeSeparator = 159,
			SdlScancodeOut = 160,
			SdlScancodeOper = 161,
			SdlScancodeClearagain = 162,
			SdlScancodeCrsel = 163,
			SdlScancodeExsel = 164,

			SdlScancodeKp00 = 176,
			SdlScancodeKp000 = 177,
			SdlScancodeThousandsseparator = 178,
			SdlScancodeDecimalseparator = 179,
			SdlScancodeCurrencyunit = 180,
			SdlScancodeCurrencysubunit = 181,
			SdlScancodeKpLeftparen = 182,
			SdlScancodeKpRightparen = 183,
			SdlScancodeKpLeftbrace = 184,
			SdlScancodeKpRightbrace = 185,
			SdlScancodeKpTab = 186,
			SdlScancodeKpBackspace = 187,
			SdlScancodeKpA = 188,
			SdlScancodeKpB = 189,
			SdlScancodeKpC = 190,
			SdlScancodeKpD = 191,
			SdlScancodeKpE = 192,
			SdlScancodeKpF = 193,
			SdlScancodeKpXor = 194,
			SdlScancodeKpPower = 195,
			SdlScancodeKpPercent = 196,
			SdlScancodeKpLess = 197,
			SdlScancodeKpGreater = 198,
			SdlScancodeKpAmpersand = 199,
			SdlScancodeKpDblampersand = 200,
			SdlScancodeKpVerticalbar = 201,
			SdlScancodeKpDblverticalbar = 202,
			SdlScancodeKpColon = 203,
			SdlScancodeKpHash = 204,
			SdlScancodeKpSpace = 205,
			SdlScancodeKpAt = 206,
			SdlScancodeKpExclam = 207,
			SdlScancodeKpMemstore = 208,
			SdlScancodeKpMemrecall = 209,
			SdlScancodeKpMemclear = 210,
			SdlScancodeKpMemadd = 211,
			SdlScancodeKpMemsubtract = 212,
			SdlScancodeKpMemmultiply = 213,
			SdlScancodeKpMemdivide = 214,
			SdlScancodeKpPlusminus = 215,
			SdlScancodeKpClear = 216,
			SdlScancodeKpClearentry = 217,
			SdlScancodeKpBinary = 218,
			SdlScancodeKpOctal = 219,
			SdlScancodeKpDecimal = 220,
			SdlScancodeKpHexadecimal = 221,

			SdlScancodeLctrl = 224,
			SdlScancodeLshift = 225,
			SdlScancodeLalt = 226,
			SdlScancodeLgui = 227,
			SdlScancodeRctrl = 228,
			SdlScancodeRshift = 229,
			SdlScancodeRalt = 230,
			SdlScancodeRgui = 231,

			SdlScancodeMode = 257,

			/* These come from the USB consumer page (0x0C) */
			SdlScancodeAudionext = 258,
			SdlScancodeAudioprev = 259,
			SdlScancodeAudiostop = 260,
			SdlScancodeAudioplay = 261,
			SdlScancodeAudiomute = 262,
			SdlScancodeMediaselect = 263,
			SdlScancodeWww = 264,
			SdlScancodeMail = 265,
			SdlScancodeCalculator = 266,
			SdlScancodeComputer = 267,
			SdlScancodeAcSearch = 268,
			SdlScancodeAcHome = 269,
			SdlScancodeAcBack = 270,
			SdlScancodeAcForward = 271,
			SdlScancodeAcStop = 272,
			SdlScancodeAcRefresh = 273,
			SdlScancodeAcBookmarks = 274,

			/* These come from other sources, and are mostly mac related */
			SdlScancodeBrightnessdown = 275,
			SdlScancodeBrightnessup = 276,
			SdlScancodeDisplayswitch = 277,
			SdlScancodeKbdillumtoggle = 278,
			SdlScancodeKbdillumdown = 279,
			SdlScancodeKbdillumup = 280,
			SdlScancodeEject = 281,
			SdlScancodeSleep = 282,

			SdlScancodeApp1 = 283,
			SdlScancodeApp2 = 284,

			/* These come from the USB consumer page (0x0C) */
			SdlScancodeAudiorewind = 285,
			SdlScancodeAudiofastforward = 286,

			/* This is not a key, simply marks the number of scancodes
			 * so that you know how big to make your arrays. */
			SdlNumScancodes = 512
		}

		#endregion

		#region SDL_keycode.h

		public const int SdlkScancodeMask = (1 << 30);
		public static SdlKeycode SDL_SCANCODE_TO_KEYCODE(SdlScancode x)
		{
			return (SdlKeycode)((int)x | SdlkScancodeMask);
		}

		public enum SdlKeycode
		{
			SdlkUnknown = 0,

			SdlkReturn = '\r',
			SdlkEscape = 27, // '\033'
			SdlkBackspace = '\b',
			SdlkTab = '\t',
			SdlkSpace = ' ',
			SdlkExclaim = '!',
			SdlkQuotedbl = '"',
			SdlkHash = '#',
			SdlkPercent = '%',
			SdlkDollar = '$',
			SdlkAmpersand = '&',
			SdlkQuote = '\'',
			SdlkLeftparen = '(',
			SdlkRightparen = ')',
			SdlkAsterisk = '*',
			SdlkPlus = '+',
			SdlkComma = ',',
			SdlkMinus = '-',
			SdlkPeriod = '.',
			SdlkSlash = '/',
			Sdlk0 = '0',
			Sdlk1 = '1',
			Sdlk2 = '2',
			Sdlk3 = '3',
			Sdlk4 = '4',
			Sdlk5 = '5',
			Sdlk6 = '6',
			Sdlk7 = '7',
			Sdlk8 = '8',
			Sdlk9 = '9',
			SdlkColon = ':',
			SdlkSemicolon = ';',
			SdlkLess = '<',
			SdlkEquals = '=',
			SdlkGreater = '>',
			SdlkQuestion = '?',
			SdlkAt = '@',
			/*
			Skip uppercase letters
			*/
			SdlkLeftbracket = '[',
			SdlkBackslash = '\\',
			SdlkRightbracket = ']',
			SdlkCaret = '^',
			SdlkUnderscore = '_',
			SdlkBackquote = '`',
			SdlkA = 'a',
			SdlkB = 'b',
			SdlkC = 'c',
			SdlkD = 'd',
			SdlkE = 'e',
			SdlkF = 'f',
			SdlkG = 'g',
			SdlkH = 'h',
			SdlkI = 'i',
			SdlkJ = 'j',
			SdlkK = 'k',
			SdlkL = 'l',
			SdlkM = 'm',
			SdlkN = 'n',
			SdlkO = 'o',
			SdlkP = 'p',
			SdlkQ = 'q',
			SdlkR = 'r',
			SdlkS = 's',
			SdlkT = 't',
			SdlkU = 'u',
			SdlkV = 'v',
			SdlkW = 'w',
			SdlkX = 'x',
			SdlkY = 'y',
			SdlkZ = 'z',

			SdlkCapslock = SdlScancode.SdlScancodeCapslock | SdlkScancodeMask,

			SdlkF1 = SdlScancode.SdlScancodeF1 | SdlkScancodeMask,
			SdlkF2 = SdlScancode.SdlScancodeF2 | SdlkScancodeMask,
			SdlkF3 = SdlScancode.SdlScancodeF3 | SdlkScancodeMask,
			SdlkF4 = SdlScancode.SdlScancodeF4 | SdlkScancodeMask,
			SdlkF5 = SdlScancode.SdlScancodeF5 | SdlkScancodeMask,
			SdlkF6 = SdlScancode.SdlScancodeF6 | SdlkScancodeMask,
			SdlkF7 = SdlScancode.SdlScancodeF7 | SdlkScancodeMask,
			SdlkF8 = SdlScancode.SdlScancodeF8 | SdlkScancodeMask,
			SdlkF9 = SdlScancode.SdlScancodeF9 | SdlkScancodeMask,
			SdlkF10 = SdlScancode.SdlScancodeF10 | SdlkScancodeMask,
			SdlkF11 = SdlScancode.SdlScancodeF11 | SdlkScancodeMask,
			SdlkF12 = SdlScancode.SdlScancodeF12 | SdlkScancodeMask,

			SdlkPrintscreen = SdlScancode.SdlScancodePrintscreen | SdlkScancodeMask,
			SdlkScrolllock = SdlScancode.SdlScancodeScrolllock | SdlkScancodeMask,
			SdlkPause = SdlScancode.SdlScancodePause | SdlkScancodeMask,
			SdlkInsert = SdlScancode.SdlScancodeInsert | SdlkScancodeMask,
			SdlkHome = SdlScancode.SdlScancodeHome | SdlkScancodeMask,
			SdlkPageup = SdlScancode.SdlScancodePageup | SdlkScancodeMask,
			SdlkDelete = 127,
			SdlkEnd = SdlScancode.SdlScancodeEnd | SdlkScancodeMask,
			SdlkPagedown = SdlScancode.SdlScancodePagedown | SdlkScancodeMask,
			SdlkRight = SdlScancode.SdlScancodeRight | SdlkScancodeMask,
			SdlkLeft = SdlScancode.SdlScancodeLeft | SdlkScancodeMask,
			SdlkDown = SdlScancode.SdlScancodeDown | SdlkScancodeMask,
			SdlkUp = SdlScancode.SdlScancodeUp | SdlkScancodeMask,

			SdlkNumlockclear = SdlScancode.SdlScancodeNumlockclear | SdlkScancodeMask,
			SdlkKpDivide = SdlScancode.SdlScancodeKpDivide | SdlkScancodeMask,
			SdlkKpMultiply = SdlScancode.SdlScancodeKpMultiply | SdlkScancodeMask,
			SdlkKpMinus = SdlScancode.SdlScancodeKpMinus | SdlkScancodeMask,
			SdlkKpPlus = SdlScancode.SdlScancodeKpPlus | SdlkScancodeMask,
			SdlkKpEnter = SdlScancode.SdlScancodeKpEnter | SdlkScancodeMask,
			SdlkKp1 = SdlScancode.SdlScancodeKp1 | SdlkScancodeMask,
			SdlkKp2 = SdlScancode.SdlScancodeKp2 | SdlkScancodeMask,
			SdlkKp3 = SdlScancode.SdlScancodeKp3 | SdlkScancodeMask,
			SdlkKp4 = SdlScancode.SdlScancodeKp4 | SdlkScancodeMask,
			SdlkKp5 = SdlScancode.SdlScancodeKp5 | SdlkScancodeMask,
			SdlkKp6 = SdlScancode.SdlScancodeKp6 | SdlkScancodeMask,
			SdlkKp7 = SdlScancode.SdlScancodeKp7 | SdlkScancodeMask,
			SdlkKp8 = SdlScancode.SdlScancodeKp8 | SdlkScancodeMask,
			SdlkKp9 = SdlScancode.SdlScancodeKp9 | SdlkScancodeMask,
			SdlkKp0 = SdlScancode.SdlScancodeKp0 | SdlkScancodeMask,
			SdlkKpPeriod = SdlScancode.SdlScancodeKpPeriod | SdlkScancodeMask,

			SdlkApplication = SdlScancode.SdlScancodeApplication | SdlkScancodeMask,
			SdlkPower = SdlScancode.SdlScancodePower | SdlkScancodeMask,
			SdlkKpEquals = SdlScancode.SdlScancodeKpEquals | SdlkScancodeMask,
			SdlkF13 = SdlScancode.SdlScancodeF13 | SdlkScancodeMask,
			SdlkF14 = SdlScancode.SdlScancodeF14 | SdlkScancodeMask,
			SdlkF15 = SdlScancode.SdlScancodeF15 | SdlkScancodeMask,
			SdlkF16 = SdlScancode.SdlScancodeF16 | SdlkScancodeMask,
			SdlkF17 = SdlScancode.SdlScancodeF17 | SdlkScancodeMask,
			SdlkF18 = SdlScancode.SdlScancodeF18 | SdlkScancodeMask,
			SdlkF19 = SdlScancode.SdlScancodeF19 | SdlkScancodeMask,
			SdlkF20 = SdlScancode.SdlScancodeF20 | SdlkScancodeMask,
			SdlkF21 = SdlScancode.SdlScancodeF21 | SdlkScancodeMask,
			SdlkF22 = SdlScancode.SdlScancodeF22 | SdlkScancodeMask,
			SdlkF23 = SdlScancode.SdlScancodeF23 | SdlkScancodeMask,
			SdlkF24 = SdlScancode.SdlScancodeF24 | SdlkScancodeMask,
			SdlkExecute = SdlScancode.SdlScancodeExecute | SdlkScancodeMask,
			SdlkHelp = SdlScancode.SdlScancodeHelp | SdlkScancodeMask,
			SdlkMenu = SdlScancode.SdlScancodeMenu | SdlkScancodeMask,
			SdlkSelect = SdlScancode.SdlScancodeSelect | SdlkScancodeMask,
			SdlkStop = SdlScancode.SdlScancodeStop | SdlkScancodeMask,
			SdlkAgain = SdlScancode.SdlScancodeAgain | SdlkScancodeMask,
			SdlkUndo = SdlScancode.SdlScancodeUndo | SdlkScancodeMask,
			SdlkCut = SdlScancode.SdlScancodeCut | SdlkScancodeMask,
			SdlkCopy = SdlScancode.SdlScancodeCopy | SdlkScancodeMask,
			SdlkPaste = SdlScancode.SdlScancodePaste | SdlkScancodeMask,
			SdlkFind = SdlScancode.SdlScancodeFind | SdlkScancodeMask,
			SdlkMute = SdlScancode.SdlScancodeMute | SdlkScancodeMask,
			SdlkVolumeup = SdlScancode.SdlScancodeVolumeup | SdlkScancodeMask,
			SdlkVolumedown = SdlScancode.SdlScancodeVolumedown | SdlkScancodeMask,
			SdlkKpComma = SdlScancode.SdlScancodeKpComma | SdlkScancodeMask,
			SdlkKpEqualsas400 =
			SdlScancode.SdlScancodeKpEqualsas400 | SdlkScancodeMask,

			SdlkAlterase = SdlScancode.SdlScancodeAlterase | SdlkScancodeMask,
			SdlkSysreq = SdlScancode.SdlScancodeSysreq | SdlkScancodeMask,
			SdlkCancel = SdlScancode.SdlScancodeCancel | SdlkScancodeMask,
			SdlkClear = SdlScancode.SdlScancodeClear | SdlkScancodeMask,
			SdlkPrior = SdlScancode.SdlScancodePrior | SdlkScancodeMask,
			SdlkReturn2 = SdlScancode.SdlScancodeReturn2 | SdlkScancodeMask,
			SdlkSeparator = SdlScancode.SdlScancodeSeparator | SdlkScancodeMask,
			SdlkOut = SdlScancode.SdlScancodeOut | SdlkScancodeMask,
			SdlkOper = SdlScancode.SdlScancodeOper | SdlkScancodeMask,
			SdlkClearagain = SdlScancode.SdlScancodeClearagain | SdlkScancodeMask,
			SdlkCrsel = SdlScancode.SdlScancodeCrsel | SdlkScancodeMask,
			SdlkExsel = SdlScancode.SdlScancodeExsel | SdlkScancodeMask,

			SdlkKp00 = SdlScancode.SdlScancodeKp00 | SdlkScancodeMask,
			SdlkKp000 = SdlScancode.SdlScancodeKp000 | SdlkScancodeMask,
			SdlkThousandsseparator =
			SdlScancode.SdlScancodeThousandsseparator | SdlkScancodeMask,
			SdlkDecimalseparator =
			SdlScancode.SdlScancodeDecimalseparator | SdlkScancodeMask,
			SdlkCurrencyunit = SdlScancode.SdlScancodeCurrencyunit | SdlkScancodeMask,
			SdlkCurrencysubunit =
			SdlScancode.SdlScancodeCurrencysubunit | SdlkScancodeMask,
			SdlkKpLeftparen = SdlScancode.SdlScancodeKpLeftparen | SdlkScancodeMask,
			SdlkKpRightparen = SdlScancode.SdlScancodeKpRightparen | SdlkScancodeMask,
			SdlkKpLeftbrace = SdlScancode.SdlScancodeKpLeftbrace | SdlkScancodeMask,
			SdlkKpRightbrace = SdlScancode.SdlScancodeKpRightbrace | SdlkScancodeMask,
			SdlkKpTab = SdlScancode.SdlScancodeKpTab | SdlkScancodeMask,
			SdlkKpBackspace = SdlScancode.SdlScancodeKpBackspace | SdlkScancodeMask,
			SdlkKpA = SdlScancode.SdlScancodeKpA | SdlkScancodeMask,
			SdlkKpB = SdlScancode.SdlScancodeKpB | SdlkScancodeMask,
			SdlkKpC = SdlScancode.SdlScancodeKpC | SdlkScancodeMask,
			SdlkKpD = SdlScancode.SdlScancodeKpD | SdlkScancodeMask,
			SdlkKpE = SdlScancode.SdlScancodeKpE | SdlkScancodeMask,
			SdlkKpF = SdlScancode.SdlScancodeKpF | SdlkScancodeMask,
			SdlkKpXor = SdlScancode.SdlScancodeKpXor | SdlkScancodeMask,
			SdlkKpPower = SdlScancode.SdlScancodeKpPower | SdlkScancodeMask,
			SdlkKpPercent = SdlScancode.SdlScancodeKpPercent | SdlkScancodeMask,
			SdlkKpLess = SdlScancode.SdlScancodeKpLess | SdlkScancodeMask,
			SdlkKpGreater = SdlScancode.SdlScancodeKpGreater | SdlkScancodeMask,
			SdlkKpAmpersand = SdlScancode.SdlScancodeKpAmpersand | SdlkScancodeMask,
			SdlkKpDblampersand =
			SdlScancode.SdlScancodeKpDblampersand | SdlkScancodeMask,
			SdlkKpVerticalbar =
			SdlScancode.SdlScancodeKpVerticalbar | SdlkScancodeMask,
			SdlkKpDblverticalbar =
			SdlScancode.SdlScancodeKpDblverticalbar | SdlkScancodeMask,
			SdlkKpColon = SdlScancode.SdlScancodeKpColon | SdlkScancodeMask,
			SdlkKpHash = SdlScancode.SdlScancodeKpHash | SdlkScancodeMask,
			SdlkKpSpace = SdlScancode.SdlScancodeKpSpace | SdlkScancodeMask,
			SdlkKpAt = SdlScancode.SdlScancodeKpAt | SdlkScancodeMask,
			SdlkKpExclam = SdlScancode.SdlScancodeKpExclam | SdlkScancodeMask,
			SdlkKpMemstore = SdlScancode.SdlScancodeKpMemstore | SdlkScancodeMask,
			SdlkKpMemrecall = SdlScancode.SdlScancodeKpMemrecall | SdlkScancodeMask,
			SdlkKpMemclear = SdlScancode.SdlScancodeKpMemclear | SdlkScancodeMask,
			SdlkKpMemadd = SdlScancode.SdlScancodeKpMemadd | SdlkScancodeMask,
			SdlkKpMemsubtract =
			SdlScancode.SdlScancodeKpMemsubtract | SdlkScancodeMask,
			SdlkKpMemmultiply =
			SdlScancode.SdlScancodeKpMemmultiply | SdlkScancodeMask,
			SdlkKpMemdivide = SdlScancode.SdlScancodeKpMemdivide | SdlkScancodeMask,
			SdlkKpPlusminus = SdlScancode.SdlScancodeKpPlusminus | SdlkScancodeMask,
			SdlkKpClear = SdlScancode.SdlScancodeKpClear | SdlkScancodeMask,
			SdlkKpClearentry = SdlScancode.SdlScancodeKpClearentry | SdlkScancodeMask,
			SdlkKpBinary = SdlScancode.SdlScancodeKpBinary | SdlkScancodeMask,
			SdlkKpOctal = SdlScancode.SdlScancodeKpOctal | SdlkScancodeMask,
			SdlkKpDecimal = SdlScancode.SdlScancodeKpDecimal | SdlkScancodeMask,
			SdlkKpHexadecimal =
			SdlScancode.SdlScancodeKpHexadecimal | SdlkScancodeMask,

			SdlkLctrl = SdlScancode.SdlScancodeLctrl | SdlkScancodeMask,
			SdlkLshift = SdlScancode.SdlScancodeLshift | SdlkScancodeMask,
			SdlkLalt = SdlScancode.SdlScancodeLalt | SdlkScancodeMask,
			SdlkLgui = SdlScancode.SdlScancodeLgui | SdlkScancodeMask,
			SdlkRctrl = SdlScancode.SdlScancodeRctrl | SdlkScancodeMask,
			SdlkRshift = SdlScancode.SdlScancodeRshift | SdlkScancodeMask,
			SdlkRalt = SdlScancode.SdlScancodeRalt | SdlkScancodeMask,
			SdlkRgui = SdlScancode.SdlScancodeRgui | SdlkScancodeMask,

			SdlkMode = SdlScancode.SdlScancodeMode | SdlkScancodeMask,

			SdlkAudionext = SdlScancode.SdlScancodeAudionext | SdlkScancodeMask,
			SdlkAudioprev = SdlScancode.SdlScancodeAudioprev | SdlkScancodeMask,
			SdlkAudiostop = SdlScancode.SdlScancodeAudiostop | SdlkScancodeMask,
			SdlkAudioplay = SdlScancode.SdlScancodeAudioplay | SdlkScancodeMask,
			SdlkAudiomute = SdlScancode.SdlScancodeAudiomute | SdlkScancodeMask,
			SdlkMediaselect = SdlScancode.SdlScancodeMediaselect | SdlkScancodeMask,
			SdlkWww = SdlScancode.SdlScancodeWww | SdlkScancodeMask,
			SdlkMail = SdlScancode.SdlScancodeMail | SdlkScancodeMask,
			SdlkCalculator = SdlScancode.SdlScancodeCalculator | SdlkScancodeMask,
			SdlkComputer = SdlScancode.SdlScancodeComputer | SdlkScancodeMask,
			SdlkAcSearch = SdlScancode.SdlScancodeAcSearch | SdlkScancodeMask,
			SdlkAcHome = SdlScancode.SdlScancodeAcHome | SdlkScancodeMask,
			SdlkAcBack = SdlScancode.SdlScancodeAcBack | SdlkScancodeMask,
			SdlkAcForward = SdlScancode.SdlScancodeAcForward | SdlkScancodeMask,
			SdlkAcStop = SdlScancode.SdlScancodeAcStop | SdlkScancodeMask,
			SdlkAcRefresh = SdlScancode.SdlScancodeAcRefresh | SdlkScancodeMask,
			SdlkAcBookmarks = SdlScancode.SdlScancodeAcBookmarks | SdlkScancodeMask,

			SdlkBrightnessdown =
			SdlScancode.SdlScancodeBrightnessdown | SdlkScancodeMask,
			SdlkBrightnessup = SdlScancode.SdlScancodeBrightnessup | SdlkScancodeMask,
			SdlkDisplayswitch = SdlScancode.SdlScancodeDisplayswitch | SdlkScancodeMask,
			SdlkKbdillumtoggle =
			SdlScancode.SdlScancodeKbdillumtoggle | SdlkScancodeMask,
			SdlkKbdillumdown = SdlScancode.SdlScancodeKbdillumdown | SdlkScancodeMask,
			SdlkKbdillumup = SdlScancode.SdlScancodeKbdillumup | SdlkScancodeMask,
			SdlkEject = SdlScancode.SdlScancodeEject | SdlkScancodeMask,
			SdlkSleep = SdlScancode.SdlScancodeSleep | SdlkScancodeMask,
			SdlkApp1 = SdlScancode.SdlScancodeApp1 | SdlkScancodeMask,
			SdlkApp2 = SdlScancode.SdlScancodeApp2 | SdlkScancodeMask,

			SdlkAudiorewind = SdlScancode.SdlScancodeAudiorewind | SdlkScancodeMask,
			SdlkAudiofastforward = SdlScancode.SdlScancodeAudiofastforward | SdlkScancodeMask
		}

		/* Key modifiers (bitfield) */
		[Flags]
		public enum SdlKeymod : ushort
		{
			KmodNone = 0x0000,
			KmodLshift = 0x0001,
			KmodRshift = 0x0002,
			KmodLctrl = 0x0040,
			KmodRctrl = 0x0080,
			KmodLalt = 0x0100,
			KmodRalt = 0x0200,
			KmodLgui = 0x0400,
			KmodRgui = 0x0800,
			KmodNum = 0x1000,
			KmodCaps = 0x2000,
			KmodMode = 0x4000,
			KmodReserved = 0x8000,

			/* These are defines in the SDL headers */
			KmodCtrl = (KmodLctrl | KmodRctrl),
			KmodShift = (KmodLshift | KmodRshift),
			KmodAlt = (KmodLalt | KmodRalt),
			KmodGui = (KmodLgui | KmodRgui)
		}

		#endregion

		#region SDL_keyboard.h

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlKeysym
		{
			public SdlScancode scancode;
			public SdlKeycode sym;
			public SdlKeymod mod; /* UInt16 */
			public UInt32 unicode; /* Deprecated */
		}

		/* Get the window which has kbd focus */
		/* Return type is an SDL_Window pointer */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetKeyboardFocus();

		/* Get a snapshot of the keyboard state. */
		/* Return value is a pointer to a UInt8 array */
		/* Numkeys returns the size of the array if non-null */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetKeyboardState(out int numkeys);

		/* Get the current key modifier state for the keyboard. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlKeymod SDL_GetModState();

		/* Set the current key modifier state */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetModState(SdlKeymod modstate);

		/* Get the key code corresponding to the given scancode
		 * with the current keyboard layout.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlKeycode SDL_GetKeyFromScancode(SdlScancode scancode);

		/* Get the scancode for the given keycode */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlScancode SDL_GetScancodeFromKey(SdlKeycode key);

		/* Wrapper for SDL_GetScancodeName */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetScancodeName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetScancodeName(SdlScancode scancode);
		public static string SDL_GetScancodeName(SdlScancode scancode)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_GetScancodeName(scancode)
			);
		}

		/* Get a scancode from a human-readable name */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetScancodeFromName", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlScancode INTERNAL_SDL_GetScancodeFromName(
			byte* name
		);
		public static unsafe SdlScancode SDL_GetScancodeFromName(string name)
		{
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];
			return INTERNAL_SDL_GetScancodeFromName(
				Utf8Encode(name, utf8Name, utf8NameBufSize)
			);
		}

		/* Wrapper for SDL_GetKeyName */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetKeyName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetKeyName(SdlKeycode key);
		public static string SDL_GetKeyName(SdlKeycode key)
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetKeyName(key));
		}

		/* Get a key code from a human-readable name */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetKeyFromName", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlKeycode INTERNAL_SDL_GetKeyFromName(
			byte* name
		);
		public static unsafe SdlKeycode SDL_GetKeyFromName(string name)
		{
			int utf8NameBufSize = Utf8Size(name);
			byte* utf8Name = stackalloc byte[utf8NameBufSize];
			return INTERNAL_SDL_GetKeyFromName(
				Utf8Encode(name, utf8Name, utf8NameBufSize)
			);
		}

		/* Start accepting Unicode text input events, show keyboard */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_StartTextInput();

		/* Check if unicode input events are enabled */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsTextInputActive();

		/* Stop receiving any text input events, hide onscreen kbd */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_StopTextInput();

		/* Set the rectangle used for text input, hint for IME */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetTextInputRect(ref SdlRect rect);

		/* Does the platform support an on-screen keyboard? */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasScreenKeyboardSupport();

		/* Is the on-screen keyboard shown for a given window? */
		/* window is an SDL_Window pointer */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsScreenKeyboardShown(IntPtr window);

		#endregion

		#region SDL_mouse.c

		/* Note: SDL_Cursor is a typedef normally. We'll treat it as
		 * an IntPtr, because C# doesn't do typedefs. Yay!
		 */

		/* System cursor types */
		public enum SdlSystemCursor
		{
			SdlSystemCursorArrow,	// Arrow
			SdlSystemCursorIbeam,	// I-beam
			SdlSystemCursorWait,		// Wait
			SdlSystemCursorCrosshair,	// Crosshair
			SdlSystemCursorWaitarrow,	// Small wait cursor (or Wait if not available)
			SdlSystemCursorSizenwse,	// Double arrow pointing northwest and southeast
			SdlSystemCursorSizenesw,	// Double arrow pointing northeast and southwest
			SdlSystemCursorSizewe,	// Double arrow pointing west and east
			SdlSystemCursorSizens,	// Double arrow pointing north and south
			SdlSystemCursorSizeall,	// Four pointed arrow pointing north, south, east, and west
			SdlSystemCursorNo,		// Slashed circle or crossbones
			SdlSystemCursorHand,		// Hand
			SdlNumSystemCursors
		}

		/* Get the window which currently has mouse focus */
		/* Return value is an SDL_Window pointer */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetMouseFocus();

		/* Get the current state of the mouse */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetMouseState(out int x, out int y);

		/* Get the current state of the mouse */
		/* This overload allows for passing NULL to x */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetMouseState(IntPtr x, out int y);

		/* Get the current state of the mouse */
		/* This overload allows for passing NULL to y */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetMouseState(out int x, IntPtr y);

		/* Get the current state of the mouse */
		/* This overload allows for passing NULL to both x and y */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetMouseState(IntPtr x, IntPtr y);

		/* Get the current state of the mouse, in relation to the desktop.
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetGlobalMouseState(out int x, out int y);

		/* Get the current state of the mouse, in relation to the desktop.
		 * Only available in 2.0.4 or higher.
		 * This overload allows for passing NULL to x.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetGlobalMouseState(IntPtr x, out int y);

		/* Get the current state of the mouse, in relation to the desktop.
		 * Only available in 2.0.4 or higher.
		 * This overload allows for passing NULL to y.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetGlobalMouseState(out int x, IntPtr y);

		/* Get the current state of the mouse, in relation to the desktop.
		 * Only available in 2.0.4 or higher.
		 * This overload allows for passing NULL to both x and y
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetGlobalMouseState(IntPtr x, IntPtr y);

		/* Get the mouse state with relative coords*/
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetRelativeMouseState(out int x, out int y);

		/* Set the mouse cursor's position (within a window) */
		/* window is an SDL_Window pointer */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_WarpMouseInWindow(IntPtr window, int x, int y);

		/* Set the mouse cursor's position in global screen space.
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_WarpMouseGlobal(int x, int y);

		/* Enable/Disable relative mouse mode (grabs mouse, rel coords) */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SetRelativeMouseMode(SdlBool enabled);

		/* Capture the mouse, to track input outside an SDL window.
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_CaptureMouse(SdlBool enabled);

		/* Query if the relative mouse mode is enabled */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GetRelativeMouseMode();

		/* Create a cursor from bitmap data (amd mask) in MSB format.
		 * data and mask are byte arrays, and w must be a multiple of 8.
		 * return value is an SDL_Cursor pointer.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateCursor(
			IntPtr data,
			IntPtr mask,
			int w,
			int h,
			int hotX,
			int hotY
		);

		/* Create a cursor from an SDL_Surface.
		 * IntPtr refers to an SDL_Cursor*, surface to an SDL_Surface*
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateColorCursor(
			IntPtr surface,
			int hotX,
			int hotY
		);

		/* Create a cursor from a system cursor id.
		 * return value is an SDL_Cursor pointer
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_CreateSystemCursor(SdlSystemCursor id);

		/* Set the active cursor.
		 * cursor is an SDL_Cursor pointer
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetCursor(IntPtr cursor);

		/* Return the active cursor
		 * return value is an SDL_Cursor pointer
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetCursor();

		/* Frees a cursor created with one of the CreateCursor functions.
		 * cursor in an SDL_Cursor pointer
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeCursor(IntPtr cursor);

		/* Toggle whether or not the cursor is shown */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_ShowCursor(int toggle);

		public static uint SDL_BUTTON(uint x)
		{
			// If only there were a better way of doing this in C#
			return (uint) (1 << ((int) x - 1));
		}

		public const uint SdlButtonLeft =	1;
		public const uint SdlButtonMiddle =	2;
		public const uint SdlButtonRight =	3;
		public const uint SdlButtonX1 =	4;
		public const uint SdlButtonX2 =	5;
		public static readonly UInt32 SdlButtonLmask =	SDL_BUTTON(SdlButtonLeft);
		public static readonly UInt32 SdlButtonMmask =	SDL_BUTTON(SdlButtonMiddle);
		public static readonly UInt32 SdlButtonRmask =	SDL_BUTTON(SdlButtonRight);
		public static readonly UInt32 SdlButtonX1Mask =	SDL_BUTTON(SdlButtonX1);
		public static readonly UInt32 SdlButtonX2Mask =	SDL_BUTTON(SdlButtonX2);

		#endregion

		#region SDL_touch.h

		public const uint SdlTouchMouseid = uint.MaxValue;

		public struct SdlFinger
		{
			public long Id; // SDL_FingerID
			public float X;
			public float Y;
			public float Pressure;
		}

		/* Only available in 2.0.10 or higher. */
		public enum SdlTouchDeviceType
		{
			SdlTouchDeviceInvalid = -1,
			SdlTouchDeviceDirect,            /* touch screen with window-relative coordinates */
			SdlTouchDeviceIndirectAbsolute, /* trackpad with absolute device coordinates */
			SdlTouchDeviceIndirectRelative  /* trackpad with screen cursor-relative coordinates */
		}

		/**
		 *  \brief Get the number of registered touch devices.
 		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumTouchDevices();

		/**
		 *  \brief Get the touch ID with the given index, or 0 if the index is invalid.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern long SDL_GetTouchDevice(int index);

		/**
		 *  \brief Get the number of active fingers for a given touch device.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumTouchFingers(long touchId);

		/**
		 *  \brief Get the finger object of the given touch, with the given index.
		 *  Returns pointer to SDL_Finger.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetTouchFinger(long touchId, int index);

		/* Only available in 2.0.10 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlTouchDeviceType SDL_GetTouchDeviceType(Int64 touchId);

		#endregion

		#region SDL_joystick.h

		public const byte SdlHatCentered =	0x00;
		public const byte SdlHatUp =		0x01;
		public const byte SdlHatRight =	0x02;
		public const byte SdlHatDown =	0x04;
		public const byte SdlHatLeft =	0x08;
		public const byte SdlHatRightup =	SdlHatRight | SdlHatUp;
		public const byte SdlHatRightdown =	SdlHatRight | SdlHatDown;
		public const byte SdlHatLeftup =	SdlHatLeft | SdlHatUp;
		public const byte SdlHatLeftdown =	SdlHatLeft | SdlHatDown;

		public enum SdlJoystickPowerLevel
		{
			SdlJoystickPowerUnknown = -1,
			SdlJoystickPowerEmpty,
			SdlJoystickPowerLow,
			SdlJoystickPowerMedium,
			SdlJoystickPowerFull,
			SdlJoystickPowerWired,
			SdlJoystickPowerMax
		}

		public enum SdlJoystickType
		{
			SdlJoystickTypeUnknown,
			SdlJoystickTypeGamecontroller,
			SdlJoystickTypeWheel,
			SdlJoystickTypeArcadeStick,
			SdlJoystickTypeFlightStick,
			SdlJoystickTypeDancePad,
			SdlJoystickTypeGuitar,
			SdlJoystickTypeDrumKit,
			SdlJoystickTypeArcadePad
		}

		/* Only available in 2.0.14 or higher. */
		public const float SdlIphoneMaxGforce = 5.0f;

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.9 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickRumble(
			IntPtr joystick,
			UInt16 lowFrequencyRumble,
			UInt16 highFrequencyRumble,
			UInt32 durationMs
		);

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickRumbleTriggers(
			IntPtr joystick,
			UInt16 leftRumble,
			UInt16 rightRumble,
			UInt32 durationMs
		);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_JoystickClose(IntPtr joystick);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickEventState(int state);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern short SDL_JoystickGetAxis(
			IntPtr joystick,
			int axis
		);

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_JoystickGetAxisInitialState(
			IntPtr joystick,
			int axis,
			out ushort state
		);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickGetBall(
			IntPtr joystick,
			int ball,
			out int dx,
			out int dy
		);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern byte SDL_JoystickGetButton(
			IntPtr joystick,
			int button
		);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern byte SDL_JoystickGetHat(
			IntPtr joystick,
			int hat
		);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, EntryPoint = "SDL_JoystickName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_JoystickName(
			IntPtr joystick
		);
		public static string SDL_JoystickName(IntPtr joystick)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_JoystickName(joystick)
			);
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_JoystickNameForIndex", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_JoystickNameForIndex(
			int deviceIndex
		);
		public static string SDL_JoystickNameForIndex(int deviceIndex)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_JoystickNameForIndex(deviceIndex)
			);
		}

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickNumAxes(IntPtr joystick);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickNumBalls(IntPtr joystick);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickNumButtons(IntPtr joystick);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickNumHats(IntPtr joystick);

		/* IntPtr refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_JoystickOpen(int deviceIndex);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_JoystickUpdate();

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_NumJoysticks();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Guid SDL_JoystickGetDeviceGUID(
			int deviceIndex
		);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Guid SDL_JoystickGetGUID(
			IntPtr joystick
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_JoystickGetGUIDString(
			Guid guid,
			byte[] pszGuid,
			int cbGuid
		);

		[DllImport(NativeLibName, EntryPoint = "SDL_JoystickGetGUIDFromString", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe Guid INTERNAL_SDL_JoystickGetGUIDFromString(
			byte* pchGuid
		);
		public static unsafe Guid SDL_JoystickGetGUIDFromString(string pchGuid)
		{
			int utf8PchGuidBufSize = Utf8Size(pchGuid);
			byte* utf8PchGuid = stackalloc byte[utf8PchGuidBufSize];
			return INTERNAL_SDL_JoystickGetGUIDFromString(
				Utf8Encode(pchGuid, utf8PchGuid, utf8PchGuidBufSize)
			);
		}

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_JoystickGetDeviceVendor(int deviceIndex);

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_JoystickGetDeviceProduct(int deviceIndex);

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_JoystickGetDeviceProductVersion(int deviceIndex);

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlJoystickType SDL_JoystickGetDeviceType(int deviceIndex);

		/* int refers to an SDL_JoystickID.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickGetDeviceInstanceID(int deviceIndex);

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_JoystickGetVendor(IntPtr joystick);

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_JoystickGetProduct(IntPtr joystick);

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_JoystickGetProductVersion(IntPtr joystick);

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_JoystickGetSerial", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_JoystickGetSerial(
			IntPtr joystick
		);
		public static string SDL_JoystickGetSerial(
			IntPtr joystick
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_JoystickGetSerial(joystick)
			);
		}

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlJoystickType SDL_JoystickGetType(IntPtr joystick);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_JoystickGetAttached(IntPtr joystick);

		/* int refers to an SDL_JoystickID, joystick to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickInstanceID(IntPtr joystick);

		/* joystick refers to an SDL_Joystick*.
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlJoystickPowerLevel SDL_JoystickCurrentPowerLevel(
			IntPtr joystick
		);

		/* int refers to an SDL_JoystickID, IntPtr to an SDL_Joystick*.
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_JoystickFromInstanceID(int instanceId);

		/* Only available in 2.0.7 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LockJoysticks();

		/* Only available in 2.0.7 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_UnlockJoysticks();

		/* IntPtr refers to an SDL_Joystick*.
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_JoystickFromPlayerIndex(int playerIndex);

		/* IntPtr refers to an SDL_Joystick*.
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_JoystickSetPlayerIndex(
			IntPtr joystick,
			int playerIndex
		);

		/* Int32 refers to an SDL_JoystickType.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickAttachVirtual(
			Int32 type,
			int naxes,
			int nbuttons,
			int nhats
		);

		/* Only available in 2.0.14 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickDetachVirtual(int deviceIndex);

		/* Only available in 2.0.14 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_JoystickIsVirtual(int deviceIndex);

		/* IntPtr refers to an SDL_Joystick*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickSetVirtualAxis(
			IntPtr joystick,
			int axis,
			Int16 value
		);

		/* IntPtr refers to an SDL_Joystick*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickSetVirtualButton(
			IntPtr joystick,
			int button,
			byte value
		);

		/* IntPtr refers to an SDL_Joystick*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickSetVirtualHat(
			IntPtr joystick,
			int hat,
			byte value
		);

		/* IntPtr refers to an SDL_Joystick*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_JoystickHasLED(IntPtr joystick);

		/* IntPtr refers to an SDL_Joystick*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickSetLED(
			IntPtr joystick,
			byte red,
			byte green,
			byte blue
		);

		#endregion

		#region SDL_gamecontroller.h

		public enum SdlGameControllerBindType
		{
			SdlControllerBindtypeNone,
			SdlControllerBindtypeButton,
			SdlControllerBindtypeAxis,
			SdlControllerBindtypeHat
		}

		public enum SdlGameControllerAxis
		{
			SdlControllerAxisInvalid = -1,
			SdlControllerAxisLeftx,
			SdlControllerAxisLefty,
			SdlControllerAxisRightx,
			SdlControllerAxisRighty,
			SdlControllerAxisTriggerleft,
			SdlControllerAxisTriggerright,
			SdlControllerAxisMax
		}

		public enum SdlGameControllerButton
		{
			SdlControllerButtonInvalid = -1,
			SdlControllerButtonA,
			SdlControllerButtonB,
			SdlControllerButtonX,
			SdlControllerButtonY,
			SdlControllerButtonBack,
			SdlControllerButtonGuide,
			SdlControllerButtonStart,
			SdlControllerButtonLeftstick,
			SdlControllerButtonRightstick,
			SdlControllerButtonLeftshoulder,
			SdlControllerButtonRightshoulder,
			SdlControllerButtonDpadUp,
			SdlControllerButtonDpadDown,
			SdlControllerButtonDpadLeft,
			SdlControllerButtonDpadRight,
			SdlControllerButtonMisc1,
			SdlControllerButtonPaddle1,
			SdlControllerButtonPaddle2,
			SdlControllerButtonPaddle3,
			SdlControllerButtonPaddle4,
			SdlControllerButtonTouchpad,
			SdlControllerButtonMax,
		}

		public enum SdlGameControllerType
		{
			SdlControllerTypeUnknown = 0,
			SdlControllerTypeXbox360,
			SdlControllerTypeXboxone,
			SdlControllerTypePs3,
			SdlControllerTypePs4,
			SdlControllerTypeNintendoSwitchPro,
			SdlControllerTypeVirtual,	/* Requires >= 2.0.14 */
			SdlControllerTypePs5,	/* Requires >= 2.0.14 */
		}

		// FIXME: I'd rather this somehow be private...
		[StructLayout(LayoutKind.Sequential)]
		public struct InternalGameControllerButtonBindHat
		{
			public int hat;
			public int hat_mask;
		}

		// FIXME: I'd rather this somehow be private...
		[StructLayout(LayoutKind.Explicit)]
		public struct InternalGameControllerButtonBindUnion
		{
			[FieldOffset(0)]
			public int button;
			[FieldOffset(0)]
			public int axis;
			[FieldOffset(0)]
			public InternalGameControllerButtonBindHat hat;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlGameControllerButtonBind
		{
			public SdlGameControllerBindType bindType;
			public InternalGameControllerButtonBindUnion value;
		}

		/* This exists to deal with C# being stupid about blittable types. */
		[StructLayout(LayoutKind.Sequential)]
		private struct InternalSdlGameControllerButtonBind
		{
			public int bindType;
			/* Largest data type in the union is two ints in size */
			public int unionVal0;
			public int unionVal1;
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerAddMapping", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe int INTERNAL_SDL_GameControllerAddMapping(
			byte* mappingString
		);
		public static unsafe int SDL_GameControllerAddMapping(
			string mappingString
		) {
			byte* utf8MappingString = Utf8EncodeHeap(mappingString);
			int result = INTERNAL_SDL_GameControllerAddMapping(
				utf8MappingString
			);
			Marshal.FreeHGlobal((IntPtr) utf8MappingString);
			return result;
		}

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerNumMappings();

		/* Only available in 2.0.6 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerMappingForIndex", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerMappingForIndex(int mappingIndex);
		public static string SDL_GameControllerMappingForIndex(int mappingIndex)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerMappingForIndex(
					mappingIndex
				)
			);
		}

		/* THIS IS AN RWops FUNCTION! */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerAddMappingsFromRW", CallingConvention = CallingConvention.Cdecl)]
		private static extern int INTERNAL_SDL_GameControllerAddMappingsFromRW(
			IntPtr rw,
			int freerw
		);
		public static int SDL_GameControllerAddMappingsFromFile(string file)
		{
			IntPtr rwops = SDL_RWFromFile(file, "rb");
			return INTERNAL_SDL_GameControllerAddMappingsFromRW(rwops, 1);
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerMappingForGUID", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerMappingForGUID(
			Guid guid
		);
		public static string SDL_GameControllerMappingForGUID(Guid guid)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerMappingForGUID(guid)
			);
		}

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerMapping", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerMapping(
			IntPtr gamecontroller
		);
		public static string SDL_GameControllerMapping(
			IntPtr gamecontroller
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerMapping(
					gamecontroller
				)
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsGameController(int joystickIndex);

		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerNameForIndex", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerNameForIndex(
			int joystickIndex
		);
		public static string SDL_GameControllerNameForIndex(
			int joystickIndex
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerNameForIndex(joystickIndex)
			);
		}

		/* Only available in 2.0.9 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerMappingForDeviceIndex", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerMappingForDeviceIndex(
			int joystickIndex
		);
		public static string SDL_GameControllerMappingForDeviceIndex(
			int joystickIndex
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerMappingForDeviceIndex(joystickIndex)
			);
		}

		/* IntPtr refers to an SDL_GameController* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GameControllerOpen(int joystickIndex);

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerName(
			IntPtr gamecontroller
		);
		public static string SDL_GameControllerName(
			IntPtr gamecontroller
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerName(gamecontroller)
			);
		}

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_GameControllerGetVendor(
			IntPtr gamecontroller
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_GameControllerGetProduct(
			IntPtr gamecontroller
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.6 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern ushort SDL_GameControllerGetProductVersion(
			IntPtr gamecontroller
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerGetSerial", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerGetSerial(
			IntPtr gamecontroller
		);
		public static string SDL_GameControllerGetSerial(
			IntPtr gamecontroller
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerGetSerial(gamecontroller)
			);
		}

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GameControllerGetAttached(
			IntPtr gamecontroller
		);

		/* IntPtr refers to an SDL_Joystick*
		 * gamecontroller refers to an SDL_GameController*
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GameControllerGetJoystick(
			IntPtr gamecontroller
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerEventState(int state);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GameControllerUpdate();

		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerGetAxisFromString", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlGameControllerAxis INTERNAL_SDL_GameControllerGetAxisFromString(
			byte* pchString
		);
		public static unsafe SdlGameControllerAxis SDL_GameControllerGetAxisFromString(
			string pchString
		) {
			int utf8PchStringBufSize = Utf8Size(pchString);
			byte* utf8PchString = stackalloc byte[utf8PchStringBufSize];
			return INTERNAL_SDL_GameControllerGetAxisFromString(
				Utf8Encode(pchString, utf8PchString, utf8PchStringBufSize)
			);
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerGetStringForAxis", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerGetStringForAxis(
			SdlGameControllerAxis axis
		);
		public static string SDL_GameControllerGetStringForAxis(
			SdlGameControllerAxis axis
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerGetStringForAxis(
					axis
				)
			);
		}

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerGetBindForAxis", CallingConvention = CallingConvention.Cdecl)]
		private static extern InternalSdlGameControllerButtonBind INTERNAL_SDL_GameControllerGetBindForAxis(
			IntPtr gamecontroller,
			SdlGameControllerAxis axis
		);
		public static SdlGameControllerButtonBind SDL_GameControllerGetBindForAxis(
			IntPtr gamecontroller,
			SdlGameControllerAxis axis
		) {
			// This is guaranteed to never be null
			InternalSdlGameControllerButtonBind dumb = INTERNAL_SDL_GameControllerGetBindForAxis(
				gamecontroller,
				axis
			);
			SdlGameControllerButtonBind result = new SdlGameControllerButtonBind();
			result.bindType = (SdlGameControllerBindType) dumb.bindType;
			result.value.hat.hat = dumb.unionVal0;
			result.value.hat.hat_mask = dumb.unionVal1;
			return result;
		}

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern short SDL_GameControllerGetAxis(
			IntPtr gamecontroller,
			SdlGameControllerAxis axis
		);

		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerGetButtonFromString", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe SdlGameControllerButton INTERNAL_SDL_GameControllerGetButtonFromString(
			byte* pchString
		);
		public static unsafe SdlGameControllerButton SDL_GameControllerGetButtonFromString(
			string pchString
		) {
			int utf8PchStringBufSize = Utf8Size(pchString);
			byte* utf8PchString = stackalloc byte[utf8PchStringBufSize];
			return INTERNAL_SDL_GameControllerGetButtonFromString(
				Utf8Encode(pchString, utf8PchString, utf8PchStringBufSize)
			);
		}

		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerGetStringForButton", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GameControllerGetStringForButton(
			SdlGameControllerButton button
		);
		public static string SDL_GameControllerGetStringForButton(
			SdlGameControllerButton button
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GameControllerGetStringForButton(button)
			);
		}

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, EntryPoint = "SDL_GameControllerGetBindForButton", CallingConvention = CallingConvention.Cdecl)]
		private static extern InternalSdlGameControllerButtonBind INTERNAL_SDL_GameControllerGetBindForButton(
			IntPtr gamecontroller,
			SdlGameControllerButton button
		);
		public static SdlGameControllerButtonBind SDL_GameControllerGetBindForButton(
			IntPtr gamecontroller,
			SdlGameControllerButton button
		) {
			// This is guaranteed to never be null
			InternalSdlGameControllerButtonBind dumb = INTERNAL_SDL_GameControllerGetBindForButton(
				gamecontroller,
				button
			);
			SdlGameControllerButtonBind result = new SdlGameControllerButtonBind();
			result.bindType = (SdlGameControllerBindType) dumb.bindType;
			result.value.hat.hat = dumb.unionVal0;
			result.value.hat.hat_mask = dumb.unionVal1;
			return result;
		}

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern byte SDL_GameControllerGetButton(
			IntPtr gamecontroller,
			SdlGameControllerButton button
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.9 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerRumble(
			IntPtr gamecontroller,
			UInt16 lowFrequencyRumble,
			UInt16 highFrequencyRumble,
			UInt32 durationMs
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerRumbleTriggers(
			IntPtr gamecontroller,
			UInt16 leftRumble,
			UInt16 rightRumble,
			UInt32 durationMs
		);

		/* gamecontroller refers to an SDL_GameController* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GameControllerClose(
			IntPtr gamecontroller
		);

		/* int refers to an SDL_JoystickID, IntPtr to an SDL_GameController*.
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GameControllerFromInstanceID(int joyid);

		/* Only available in 2.0.11 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlGameControllerType SDL_GameControllerTypeForIndex(
			int joystickIndex
		);

		/* IntPtr refers to an SDL_GameController*.
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlGameControllerType SDL_GameControllerGetType(
			IntPtr gamecontroller
		);

		/* IntPtr refers to an SDL_GameController*.
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GameControllerFromPlayerIndex(
			int playerIndex
		);

		/* IntPtr refers to an SDL_GameController*.
		 * Only available in 2.0.11 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_GameControllerSetPlayerIndex(
			IntPtr gamecontroller,
			int playerIndex
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GameControllerHasLED(
			IntPtr gamecontroller
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerSetLED(
			IntPtr gamecontroller,
			byte red,
			byte green,
			byte blue
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GameControllerHasAxis(
			IntPtr gamecontroller,
			SdlGameControllerAxis axis
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GameControllerHasButton(
			IntPtr gamecontroller,
			SdlGameControllerButton button
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerGetNumTouchpads(
			IntPtr gamecontroller
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerGetNumTouchpadFingers(
			IntPtr gamecontroller,
			int touchpad
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerGetTouchpadFinger(
			IntPtr gamecontroller,
			int touchpad,
			int finger,
			out byte state,
			out float x,
			out float y,
			out float pressure
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GameControllerHasSensor(
			IntPtr gamecontroller,
			SdlSensorType type
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerSetSensorEnabled(
			IntPtr gamecontroller,
			SdlSensorType type,
			SdlBool enabled
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GameControllerIsSensorEnabled(
			IntPtr gamecontroller,
			SdlSensorType type
		);

		/* gamecontroller refers to an SDL_GameController*.
		 * data refers to a float*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GameControllerGetSensorData(
			IntPtr gamecontroller,
			SdlSensorType type,
			IntPtr data,
			int numValues
		);

		#endregion

		#region SDL_haptic.h

		/* SDL_HapticEffect type */
		public const ushort SdlHapticConstant =	(1 << 0);
		public const ushort SdlHapticSine =		(1 << 1);
		public const ushort SdlHapticLeftright =	(1 << 2);
		public const ushort SdlHapticTriangle =	(1 << 3);
		public const ushort SdlHapticSawtoothup =	(1 << 4);
		public const ushort SdlHapticSawtoothdown =	(1 << 5);
		public const ushort SdlHapticSpring =		(1 << 7);
		public const ushort SdlHapticDamper =		(1 << 8);
		public const ushort SdlHapticInertia =	(1 << 9);
		public const ushort SdlHapticFriction =	(1 << 10);
		public const ushort SdlHapticCustom =		(1 << 11);
		public const ushort SdlHapticGain =		(1 << 12);
		public const ushort SdlHapticAutocenter =	(1 << 13);
		public const ushort SdlHapticStatus =		(1 << 14);
		public const ushort SdlHapticPause =		(1 << 15);

		/* SDL_HapticDirection type */
		public const byte SdlHapticPolar =		0;
		public const byte SdlHapticCartesian =	1;
		public const byte SdlHapticSpherical =	2;
		public const byte SdlHapticSteeringAxis =	3; /* Requires >= 2.0.14 */

		/* SDL_HapticRunEffect */
		public const uint SdlHapticInfinity = 4294967295U;

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct SdlHapticDirection
		{
			public byte type;
			public fixed int dir[3];
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlHapticConstantData
		{
			// Header
			public ushort type;
			public SdlHapticDirection direction;
			// Replay
			public uint length;
			public ushort delay;
			// Trigger
			public ushort button;
			public ushort interval;
			// Constant
			public short level;
			// Envelope
			public ushort attack_length;
			public ushort attack_level;
			public ushort fade_length;
			public ushort fade_level;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlHapticPeriodic
		{
			// Header
			public ushort type;
			public SdlHapticDirection direction;
			// Replay
			public uint length;
			public ushort delay;
			// Trigger
			public ushort button;
			public ushort interval;
			// Periodic
			public ushort period;
			public short magnitude;
			public short offset;
			public ushort phase;
			// Envelope
			public ushort attack_length;
			public ushort attack_level;
			public ushort fade_length;
			public ushort fade_level;
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct SdlHapticCondition
		{
			// Header
			public ushort type;
			public SdlHapticDirection direction;
			// Replay
			public uint length;
			public ushort delay;
			// Trigger
			public ushort button;
			public ushort interval;
			// Condition
			public fixed ushort right_sat[3];
			public fixed ushort left_sat[3];
			public fixed short right_coeff[3];
			public fixed short left_coeff[3];
			public fixed ushort deadband[3];
			public fixed short center[3];
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlHapticRamp
		{
			// Header
			public ushort type;
			public SdlHapticDirection direction;
			// Replay
			public uint length;
			public ushort delay;
			// Trigger
			public ushort button;
			public ushort interval;
			// Ramp
			public short start;
			public short end;
			// Envelope
			public ushort attack_length;
			public ushort attack_level;
			public ushort fade_length;
			public ushort fade_level;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlHapticLeftRight
		{
			// Header
			public ushort type;
			// Replay
			public uint length;
			// Rumble
			public ushort large_magnitude;
			public ushort small_magnitude;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlHapticCustomData
		{
			// Header
			public ushort type;
			public SdlHapticDirection direction;
			// Replay
			public uint length;
			public ushort delay;
			// Trigger
			public ushort button;
			public ushort interval;
			// Custom
			public byte channels;
			public ushort period;
			public ushort samples;
			public IntPtr data; // Uint16*
			// Envelope
			public ushort attack_length;
			public ushort attack_level;
			public ushort fade_length;
			public ushort fade_level;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct SdlHapticEffect
		{
			[FieldOffset(0)]
			public ushort type;
			[FieldOffset(0)]
			public SdlHapticConstantData constant;
			[FieldOffset(0)]
			public SdlHapticPeriodic periodic;
			[FieldOffset(0)]
			public SdlHapticCondition condition;
			[FieldOffset(0)]
			public SdlHapticRamp ramp;
			[FieldOffset(0)]
			public SdlHapticLeftRight leftright;
			[FieldOffset(0)]
			public SdlHapticCustomData custom;
		}

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_HapticClose(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_HapticDestroyEffect(
			IntPtr haptic,
			int effect
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticEffectSupported(
			IntPtr haptic,
			ref SdlHapticEffect effect
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticGetEffectStatus(
			IntPtr haptic,
			int effect
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticIndex(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, EntryPoint = "SDL_HapticName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_HapticName(int deviceIndex);
		public static string SDL_HapticName(int deviceIndex)
		{
			return UTF8_ToManaged(INTERNAL_SDL_HapticName(deviceIndex));
		}

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticNewEffect(
			IntPtr haptic,
			ref SdlHapticEffect effect
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticNumAxes(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticNumEffects(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticNumEffectsPlaying(IntPtr haptic);

		/* IntPtr refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_HapticOpen(int deviceIndex);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticOpened(int deviceIndex);

		/* IntPtr refers to an SDL_Haptic*, joystick to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_HapticOpenFromJoystick(
			IntPtr joystick
		);

		/* IntPtr refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_HapticOpenFromMouse();

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticPause(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_HapticQuery(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticRumbleInit(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticRumblePlay(
			IntPtr haptic,
			float strength,
			uint length
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticRumbleStop(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticRumbleSupported(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticRunEffect(
			IntPtr haptic,
			int effect,
			uint iterations
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticSetAutocenter(
			IntPtr haptic,
			int autocenter
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticSetGain(
			IntPtr haptic,
			int gain
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticStopAll(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticStopEffect(
			IntPtr haptic,
			int effect
		);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticUnpause(IntPtr haptic);

		/* haptic refers to an SDL_Haptic* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_HapticUpdateEffect(
			IntPtr haptic,
			int effect,
			ref SdlHapticEffect data
		);

		/* joystick refers to an SDL_Joystick* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_JoystickIsHaptic(IntPtr joystick);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_MouseIsHaptic();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_NumHaptics();

		#endregion

		#region SDL_sensor.h

		/* This region is only available in 2.0.9 or higher. */

		public enum SdlSensorType
		{
			SdlSensorInvalid = -1,
			SdlSensorUnknown,
			SdlSensorAccel,
			SdlSensorGyro
		}

		public const float SdlStandardGravity = 9.80665f;

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_NumSensors();

		[DllImport(NativeLibName, EntryPoint = "SDL_SensorGetDeviceName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_SensorGetDeviceName(int deviceIndex);
		public static string SDL_SensorGetDeviceName(int deviceIndex)
		{
			return UTF8_ToManaged(INTERNAL_SDL_SensorGetDeviceName(deviceIndex));
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlSensorType SDL_SensorGetDeviceType(int deviceIndex);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SensorGetDeviceNonPortableType(int deviceIndex);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Int32 SDL_SensorGetDeviceInstanceID(int deviceIndex);

		/* IntPtr refers to an SDL_Sensor* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_SensorOpen(int deviceIndex);

		/* IntPtr refers to an SDL_Sensor* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_SensorFromInstanceID(
			Int32 instanceId
		);

		/* sensor refers to an SDL_Sensor* */
		[DllImport(NativeLibName, EntryPoint = "SDL_SensorGetName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_SensorGetName(IntPtr sensor);
		public static string SDL_SensorGetName(IntPtr sensor)
		{
			return UTF8_ToManaged(INTERNAL_SDL_SensorGetName(sensor));
		}

		/* sensor refers to an SDL_Sensor* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlSensorType SDL_SensorGetType(IntPtr sensor);

		/* sensor refers to an SDL_Sensor* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SensorGetNonPortableType(IntPtr sensor);

		/* sensor refers to an SDL_Sensor* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern Int32 SDL_SensorGetInstanceID(IntPtr sensor);

		/* sensor refers to an SDL_Sensor* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_SensorGetData(
			IntPtr sensor,
			float[] data,
			int numValues
		);

		/* sensor refers to an SDL_Sensor* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SensorClose(IntPtr sensor);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SensorUpdate();

		/* Only available in 2.0.14 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LockSensors();

		/* Only available in 2.0.14 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_UnlockSensors();

		#endregion

		#region SDL_audio.h

		public const ushort SdlAudioMaskBitsize =	0xFF;
		public const ushort SdlAudioMaskDatatype =	(1 << 8);
		public const ushort SdlAudioMaskEndian =	(1 << 12);
		public const ushort SdlAudioMaskSigned =	(1 << 15);

		public static ushort SDL_AUDIO_BITSIZE(ushort x)
		{
			return (ushort) (x & SdlAudioMaskBitsize);
		}

		public static bool SDL_AUDIO_ISFLOAT(ushort x)
		{
			return (x & SdlAudioMaskDatatype) != 0;
		}

		public static bool SDL_AUDIO_ISBIGENDIAN(ushort x)
		{
			return (x & SdlAudioMaskEndian) != 0;
		}

		public static bool SDL_AUDIO_ISSIGNED(ushort x)
		{
			return (x & SdlAudioMaskSigned) != 0;
		}

		public static bool SDL_AUDIO_ISINT(ushort x)
		{
			return (x & SdlAudioMaskDatatype) == 0;
		}

		public static bool SDL_AUDIO_ISLITTLEENDIAN(ushort x)
		{
			return (x & SdlAudioMaskEndian) == 0;
		}

		public static bool SDL_AUDIO_ISUNSIGNED(ushort x)
		{
			return (x & SdlAudioMaskSigned) == 0;
		}

		public const ushort AudioU8 =		0x0008;
		public const ushort AudioS8 =		0x8008;
		public const ushort AudioU16Lsb =	0x0010;
		public const ushort AudioS16Lsb =	0x8010;
		public const ushort AudioU16Msb =	0x1010;
		public const ushort AudioS16Msb =	0x9010;
		public const ushort AudioU16 =		AudioU16Lsb;
		public const ushort AudioS16 =		AudioS16Lsb;
		public const ushort AudioS32Lsb =	0x8020;
		public const ushort AudioS32Msb =	0x9020;
		public const ushort AudioS32 =		AudioS32Lsb;
		public const ushort AudioF32Lsb =	0x8120;
		public const ushort AudioF32Msb =	0x9120;
		public const ushort AudioF32 =		AudioF32Lsb;

		public static readonly ushort AudioU16Sys =
			BitConverter.IsLittleEndian ? AudioU16Lsb : AudioU16Msb;
		public static readonly ushort AudioS16Sys =
			BitConverter.IsLittleEndian ? AudioS16Lsb : AudioS16Msb;
		public static readonly ushort AudioS32Sys =
			BitConverter.IsLittleEndian ? AudioS32Lsb : AudioS32Msb;
		public static readonly ushort AudioF32Sys =
			BitConverter.IsLittleEndian ? AudioF32Lsb : AudioF32Msb;

		public const uint SdlAudioAllowFrequencyChange =	0x00000001;
		public const uint SdlAudioAllowFormatChange =	0x00000002;
		public const uint SdlAudioAllowChannelsChange =	0x00000004;
		public const uint SdlAudioAllowSamplesChange =	0x00000008;
		public const uint SdlAudioAllowAnyChange = (
			SdlAudioAllowFrequencyChange |
			SdlAudioAllowFormatChange |
			SdlAudioAllowChannelsChange |
			SdlAudioAllowSamplesChange
		);

		public const int SdlMixMaxvolume = 128;

		public enum SdlAudioStatus
		{
			SdlAudioStopped,
			SdlAudioPlaying,
			SdlAudioPaused
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlAudioSpec
		{
			public int freq;
			public ushort format; // SDL_AudioFormat
			public byte channels;
			public byte silence;
			public ushort samples;
			public uint size;
			public SdlAudioCallback callback;
			public IntPtr userdata; // void*
		}

		/* userdata refers to a void*, stream to a Uint8 */
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SdlAudioCallback(
			IntPtr userdata,
			IntPtr stream,
			int len
		);

		[DllImport(NativeLibName, EntryPoint = "SDL_AudioInit", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe int INTERNAL_SDL_AudioInit(
			byte* driverName
		);
		public static unsafe int SDL_AudioInit(string driverName)
		{
			int utf8DriverNameBufSize = Utf8Size(driverName);
			byte* utf8DriverName = stackalloc byte[utf8DriverNameBufSize];
			return INTERNAL_SDL_AudioInit(
				Utf8Encode(driverName, utf8DriverName, utf8DriverNameBufSize)
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_AudioQuit();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_CloseAudio();

		/* dev refers to an SDL_AudioDeviceID */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_CloseAudioDevice(uint dev);

		/* audio_buf refers to a malloc()'d buffer from SDL_LoadWAV */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeWAV(IntPtr audioBuf);

		[DllImport(NativeLibName, EntryPoint = "SDL_GetAudioDeviceName", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetAudioDeviceName(
			int index,
			int iscapture
		);
		public static string SDL_GetAudioDeviceName(
			int index,
			int iscapture
		) {
			return UTF8_ToManaged(
				INTERNAL_SDL_GetAudioDeviceName(index, iscapture)
			);
		}

		/* dev refers to an SDL_AudioDeviceID */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlAudioStatus SDL_GetAudioDeviceStatus(
			uint dev
		);

		[DllImport(NativeLibName, EntryPoint = "SDL_GetAudioDriver", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetAudioDriver(int index);
		public static string SDL_GetAudioDriver(int index)
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_GetAudioDriver(index)
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlAudioStatus SDL_GetAudioStatus();

		[DllImport(NativeLibName, EntryPoint = "SDL_GetCurrentAudioDriver", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetCurrentAudioDriver();
		public static string SDL_GetCurrentAudioDriver()
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetCurrentAudioDriver());
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumAudioDevices(int iscapture);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetNumAudioDrivers();

		/* audio_buf refers to a malloc()'d buffer, IntPtr to an SDL_AudioSpec* */
		/* THIS IS AN RWops FUNCTION! */
		[DllImport(NativeLibName, EntryPoint = "SDL_LoadWAV_RW", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_LoadWAV_RW(
			IntPtr src,
			int freesrc,
			out SdlAudioSpec spec,
			out IntPtr audioBuf,
			out uint audioLen
		);
		public static IntPtr SDL_LoadWAV(
			string file,
			out SdlAudioSpec spec,
			out IntPtr audioBuf,
			out uint audioLen
		) {
			IntPtr rwops = SDL_RWFromFile(file, "rb");
			return INTERNAL_SDL_LoadWAV_RW(
				rwops,
				1,
				out spec,
				out audioBuf,
				out audioLen
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LockAudio();

		/* dev refers to an SDL_AudioDeviceID */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_LockAudioDevice(uint dev);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_MixAudio(
			[Out()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)]
				byte[] dst,
			[In()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 2)]
				byte[] src,
			uint len,
			int volume
		);

		/* format refers to an SDL_AudioFormat */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_MixAudioFormat(
			[Out()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 3)]
				byte[] dst,
			[In()] [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 3)]
				byte[] src,
			ushort format,
			uint len,
			int volume
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_OpenAudio(
			ref SdlAudioSpec desired,
			out SdlAudioSpec obtained
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_OpenAudio(
			ref SdlAudioSpec desired,
			IntPtr obtained
		);

		/* uint refers to an SDL_AudioDeviceID */
		[DllImport(NativeLibName, EntryPoint = "SDL_OpenAudioDevice", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe uint INTERNAL_SDL_OpenAudioDevice(
			byte* device,
			int iscapture,
			ref SdlAudioSpec desired,
			out SdlAudioSpec obtained,
			int allowedChanges
		);
		public static unsafe uint SDL_OpenAudioDevice(
			string device,
			int iscapture,
			ref SdlAudioSpec desired,
			out SdlAudioSpec obtained,
			int allowedChanges
		) {
			int utf8DeviceBufSize = Utf8Size(device);
			byte* utf8Device = stackalloc byte[utf8DeviceBufSize];
			return INTERNAL_SDL_OpenAudioDevice(
				Utf8Encode(device, utf8Device, utf8DeviceBufSize),
				iscapture,
				ref desired,
				out obtained,
				allowedChanges
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_PauseAudio(int pauseOn);

		/* dev refers to an SDL_AudioDeviceID */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_PauseAudioDevice(
			uint dev,
			int pauseOn
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_UnlockAudio();

		/* dev refers to an SDL_AudioDeviceID */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_UnlockAudioDevice(uint dev);

		/* dev refers to an SDL_AudioDeviceID, data to a void*
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_QueueAudio(
			uint dev,
			IntPtr data,
			UInt32 len
		);

		/* dev refers to an SDL_AudioDeviceID, data to a void*
		 * Only available in 2.0.5 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_DequeueAudio(
			uint dev,
			IntPtr data,
			uint len
		);

		/* dev refers to an SDL_AudioDeviceID
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetQueuedAudioSize(uint dev);

		/* dev refers to an SDL_AudioDeviceID
		 * Only available in 2.0.4 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_ClearQueuedAudio(uint dev);

		/* src_format and dst_format refer to SDL_AudioFormats.
		 * IntPtr refers to an SDL_AudioStream*.
		 * Only available in 2.0.7 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_NewAudioStream(
			ushort srcFormat,
			byte srcChannels,
			int srcRate,
			ushort dstFormat,
			byte dstChannels,
			int dstRate
		);

		/* stream refers to an SDL_AudioStream*, buf to a void*.
		 * Only available in 2.0.7 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_AudioStreamPut(
			IntPtr stream,
			IntPtr buf,
			int len
		);

		/* stream refers to an SDL_AudioStream*, buf to a void*.
		 * Only available in 2.0.7 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_AudioStreamGet(
			IntPtr stream,
			IntPtr buf,
			int len
		);

		/* stream refers to an SDL_AudioStream*.
		 * Only available in 2.0.7 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_AudioStreamAvailable(IntPtr stream);

		/* stream refers to an SDL_AudioStream*.
		 * Only available in 2.0.7 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_AudioStreamClear(IntPtr stream);

		/* stream refers to an SDL_AudioStream*.
		 * Only available in 2.0.7 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_FreeAudioStream(IntPtr stream);

		#endregion

		#region SDL_timer.h

		/* System timers rely on different OS mechanisms depending on
		 * which operating system SDL2 is compiled against.
		 */

		/* Compare tick values, return true if A has passed B. Introduced in SDL 2.0.1,
		 * but does not require it (it was a macro).
		 */
		public static bool SDL_TICKS_PASSED(UInt32 a, UInt32 b)
		{
			return ((Int32)(b - a) <= 0);
		}

		/* Delays the thread's processing based on the milliseconds parameter */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_Delay(UInt32 ms);

		/* Returns the milliseconds that have passed since SDL was initialized */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt32 SDL_GetTicks();

		/* Get the current value of the high resolution counter */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt64 SDL_GetPerformanceCounter();

		/* Get the count per second of the high resolution counter */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern UInt64 SDL_GetPerformanceFrequency();

		/* param refers to a void* */
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate UInt32 SdlTimerCallback(UInt32 interval, IntPtr param);

		/* int refers to an SDL_TimerID, param to a void* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_AddTimer(
			UInt32 interval,
			SdlTimerCallback callback,
			IntPtr param
		);

		/* id refers to an SDL_TimerID */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_RemoveTimer(int id);

		#endregion

		#region SDL_system.h

		/* Windows */

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr SdlWindowsMessageHook(
			IntPtr userdata,
			IntPtr hWnd,
			uint message,
			ulong wParam,
			long lParam
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SetWindowsMessageHook(
			SdlWindowsMessageHook callback,
			IntPtr userdata
		);

		/* iOS */

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SdlIPhoneAnimationCallback(IntPtr p);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_iPhoneSetAnimationCallback(
			IntPtr window, /* SDL_Window* */
			int interval,
			SdlIPhoneAnimationCallback callback,
			IntPtr callbackParam
		);

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_iPhoneSetEventPump(SdlBool enabled);

		/* Android */

		public const int SdlAndroidExternalStorageRead = 0x01;
		public const int SdlAndroidExternalStorageWrite = 0x02;

		/* IntPtr refers to a JNIEnv* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_AndroidGetJNIEnv();

		/* IntPtr refers to a jobject */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_AndroidGetActivity();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsAndroidTV();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsChromebook();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsDeXMode();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_AndroidBackButton();

		[DllImport(NativeLibName, EntryPoint = "SDL_AndroidGetInternalStoragePath", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_AndroidGetInternalStoragePath();

		public static string SDL_AndroidGetInternalStoragePath()
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_AndroidGetInternalStoragePath()
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_AndroidGetExternalStorageState();

		[DllImport(NativeLibName, EntryPoint = "SDL_AndroidGetExternalStoragePath", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_AndroidGetExternalStoragePath();

		public static string SDL_AndroidGetExternalStoragePath()
		{
			return UTF8_ToManaged(
				INTERNAL_SDL_AndroidGetExternalStoragePath()
			);
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetAndroidSDKVersion();

		/* Only available in 2.0.14 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_AndroidRequestPermission", CallingConvention = CallingConvention.Cdecl)]
		public static unsafe extern SdlBool INTERNAL_SDL_AndroidRequestPermission(
			byte* permission
		);
		public static unsafe SdlBool SDL_AndroidRequestPermission(
			string permission
		) {
			byte* permissionPtr = Utf8EncodeHeap(permission);
			SdlBool result = INTERNAL_SDL_AndroidRequestPermission(
				permissionPtr
			);
			Marshal.FreeHGlobal((IntPtr) permissionPtr);
			return result;
		}

		/* WinRT */

		public enum SdlWinRtDeviceFamily
		{
			SdlWinrtDevicefamilyUnknown,
			SdlWinrtDevicefamilyDesktop,
			SdlWinrtDevicefamilyMobile,
			SdlWinrtDevicefamilyXbox
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlWinRtDeviceFamily SDL_WinRTGetDeviceFamily();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_IsTablet();

		#endregion

		#region SDL_syswm.h

		public enum SdlSyswmType
		{
			SdlSyswmUnknown,
			SdlSyswmWindows,
			SdlSyswmX11,
			SdlSyswmDirectfb,
			SdlSyswmCocoa,
			SdlSyswmUikit,
			SdlSyswmWayland,
			SdlSyswmMir,
			SdlSyswmWinrt,
			SdlSyswmAndroid,
			SdlSyswmVivante,
			SdlSyswmOs2,
			SdlSyswmHaiku
		}

		// FIXME: I wish these weren't public...
		[StructLayout(LayoutKind.Sequential)]
		public struct InternalWindowsWminfo
		{
			public IntPtr window; // Refers to an HWND
			public IntPtr hdc; // Refers to an HDC
			public IntPtr hinstance; // Refers to an HINSTANCE
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalWinrtWminfo
		{
			public IntPtr window; // Refers to an IInspectable*
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalX11Wminfo
		{
			public IntPtr display; // Refers to a Display*
			public IntPtr window; // Refers to a Window (XID, use ToInt64!)
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalDirectfbWminfo
		{
			public IntPtr dfb; // Refers to an IDirectFB*
			public IntPtr window; // Refers to an IDirectFBWindow*
			public IntPtr surface; // Refers to an IDirectFBSurface*
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalCocoaWminfo
		{
			public IntPtr window; // Refers to an NSWindow*
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalUikitWminfo
		{
			public IntPtr window; // Refers to a UIWindow*
			public uint framebuffer;
			public uint colorbuffer;
			public uint resolveFramebuffer;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalWaylandWminfo
		{
			public IntPtr display; // Refers to a wl_display*
			public IntPtr surface; // Refers to a wl_surface*
			public IntPtr shell_surface; // Refers to a wl_shell_surface*
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalMirWminfo
		{
			public IntPtr connection; // Refers to a MirConnection*
			public IntPtr surface; // Refers to a MirSurface*
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalAndroidWminfo
		{
			public IntPtr window; // Refers to an ANativeWindow
			public IntPtr surface; // Refers to an EGLSurface
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct InternalVivanteWminfo
		{
			public IntPtr display; // Refers to an EGLNativeDisplayType
			public IntPtr window; // Refers to an EGLNativeWindowType
		}

		/* Only available in 2.0.14 or higher. */
		[StructLayout(LayoutKind.Sequential)]
		public struct InternalOs2Wminfo
		{
			public IntPtr hwnd; /* Refers to an HWND */
			public IntPtr hwndFrame; /* Refers to an HWND */
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct InternalSysWmDriverUnion
		{
			[FieldOffset(0)]
			public InternalWindowsWminfo win;
			[FieldOffset(0)]
			public InternalWinrtWminfo winrt;
			[FieldOffset(0)]
			public InternalX11Wminfo x11;
			[FieldOffset(0)]
			public InternalDirectfbWminfo dfb;
			[FieldOffset(0)]
			public InternalCocoaWminfo cocoa;
			[FieldOffset(0)]
			public InternalUikitWminfo uikit;
			[FieldOffset(0)]
			public InternalWaylandWminfo wl;
			[FieldOffset(0)]
			public InternalMirWminfo mir;
			[FieldOffset(0)]
			public InternalAndroidWminfo android;
			[FieldOffset(0)]
			public InternalVivanteWminfo vivante;
			[FieldOffset(0)]
			public InternalOs2Wminfo os2;
			// private int dummy;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlSysWMinfo
		{
			public SdlVersion version;
			public SdlSyswmType subsystem;
			public InternalSysWmDriverUnion info;
		}

		/* window refers to an SDL_Window* */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_GetWindowWMInfo(
			IntPtr window,
			ref SdlSysWMinfo info
		);

		#endregion

		#region SDL_filesystem.h

		/* Only available in 2.0.1 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetBasePath", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr INTERNAL_SDL_GetBasePath();
		public static string SDL_GetBasePath()
		{
			return UTF8_ToManaged(INTERNAL_SDL_GetBasePath(), true);
		}

		/* Only available in 2.0.1 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_GetPrefPath", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe IntPtr INTERNAL_SDL_GetPrefPath(
			byte* org,
			byte* app
		);
		public static unsafe string SDL_GetPrefPath(string org, string app)
		{
			int utf8OrgBufSize = Utf8Size(org);
			byte* utf8Org = stackalloc byte[utf8OrgBufSize];

			int utf8AppBufSize = Utf8Size(app);
			byte* utf8App = stackalloc byte[utf8AppBufSize];

			return UTF8_ToManaged(
				INTERNAL_SDL_GetPrefPath(
					Utf8Encode(org, utf8Org, utf8OrgBufSize),
					Utf8Encode(app, utf8App, utf8AppBufSize)
				),
				true
			);
		}

		#endregion

		#region SDL_power.h

		public enum SdlPowerState
		{
			SdlPowerstateUnknown = 0,
			SdlPowerstateOnBattery,
			SdlPowerstateNoBattery,
			SdlPowerstateCharging,
			SdlPowerstateCharged
		}

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlPowerState SDL_GetPowerInfo(
			out int secs,
			out int pct
		);

		#endregion

		#region SDL_cpuinfo.h

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetCPUCount();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetCPUCacheLineSize();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasRDTSC();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasAltiVec();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasMMX();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_Has3DNow();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasSSE();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasSSE2();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasSSE3();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasSSE41();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasSSE42();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasAVX();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasAVX2();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasAVX512F();

		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern SdlBool SDL_HasNEON();

		/* Only available in 2.0.1 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SDL_GetSystemRAM();

		/* Only available in SDL 2.0.10 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern uint SDL_SIMDGetAlignment();

		/* Only available in SDL 2.0.10 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_SIMDAlloc(uint len);

		/* Only available in SDL 2.0.14 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_SIMDRealloc(IntPtr ptr, uint len);

		/* Only available in SDL 2.0.10 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_SIMDFree(IntPtr ptr);

		/* Only available in SDL 2.0.11 or higher. */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SDL_HasARMSIMD();

		#endregion

		#region SDL_locale.h

		[StructLayout(LayoutKind.Sequential)]
		public struct SdlLocale
		{
			IntPtr language;
			IntPtr country;
		}

		/* IntPtr refers to an SDL_Locale*.
		 * Only available in 2.0.14 or higher.
		 */
		[DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SDL_GetPreferredLocales();

		#endregion

		#region SDL_misc.h

		/* Only available in 2.0.14 or higher. */
		[DllImport(NativeLibName, EntryPoint = "SDL_OpenURL", CallingConvention = CallingConvention.Cdecl)]
		private static unsafe extern int INTERNAL_SDL_OpenURL(byte* url);
		public static unsafe int SDL_OpenURL(string url)
		{
			byte* urlPtr = Utf8EncodeHeap(url);
			int result = INTERNAL_SDL_OpenURL(urlPtr);
			Marshal.FreeHGlobal((IntPtr) urlPtr);
			return result;
		}

		#endregion
	}
}