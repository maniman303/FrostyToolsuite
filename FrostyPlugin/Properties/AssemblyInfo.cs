using Frosty.Core.Attributes;
using Frosty.Core.Controls.Editors;
using Frosty.Core.Controls.Overrides;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("d86d23d9-db93-4d4e-b383-5022f759aba8")]

[assembly: RegisterGlobalTypeEditor("LocalizedStringHash", typeof(FrostyLocalizedStringHashEditor))]
[assembly: RegisterGlobalTypeEditor("LocalizedStringReference", typeof(FrostyLocalizedStringReferenceEditor))]
[assembly: RegisterGlobalTypeEditor("Vec2", typeof(FrostyVec2Editor))]
[assembly: RegisterGlobalTypeEditor("Vec3", typeof(FrostyVec3Editor))]
[assembly: RegisterGlobalTypeEditor("Vec4", typeof(FrostyVec4Editor))]

[assembly: RegisterTypeOverride("RawFileDataAsset", typeof(RawFileDataAssetOverride))]
[assembly: RegisterTypeOverride("LocalizedStringId", typeof(LocalizedStringIdOverride))]
[assembly: RegisterTypeOverride("LinearTransform", typeof(LinearTransformOverride))]

