import os

base_dir = r"D:\NRO\NROKHACH\CLIENT_NRO_ARN"
controller_file = os.path.join(base_dir, r"Assets\Scripts\Controller.cs")
gamescr_file = os.path.join(base_dir, r"Assets\Scripts\GameScr.cs")

# 1. Update Controller.cs
with open(controller_file, 'r', encoding='utf-8') as f:
    content = f.read()

if "case -58: // Farm" not in content:
    content = content.replace("switch (msg.command)\n\t\t\t{", "switch (msg.command)\n\t\t\t{\n\t\t\tcase -58: // Farm\n\t\t\t\tFarmMessageHandler.GI().HandleFarmAssetMessage(msg);\n\t\t\t\tbreak;")
    content = content.replace("switch (msg.command)\n\t\t{", "switch (msg.command)\n\t\t{\n\t\t\tcase -58: // Farm\n\t\t\t\tFarmMessageHandler.GI().HandleFarmAssetMessage(msg);\n\t\t\t\tbreak;")

    if "FarmMessageHandler" not in content:
        # Just in case the switch formatting is different
        content = content.replace("switch (msg.command) {", "switch (msg.command) {\n\t\t\tcase -58: // Farm\n\t\t\t\tFarmMessageHandler.GI().HandleFarmAssetMessage(msg);\n\t\t\t\tbreak;")

with open(controller_file, 'w', encoding='utf-8') as f:
    f.write(content)

# 2. Update GameScr.cs
with open(gamescr_file, 'r', encoding='utf-8') as f:
    content = f.read()

if "CloudGarden.Paint" not in content:
    # Insert CloudGarden.Paint in paint() after paint Npc
    content = content.replace("for (int m = 0; m < vNpc.size(); m++)\n\t\t\t\t{", "for (int m = 0; m < vNpc.size(); m++)\n\t\t\t\t{")
    # Actually, a simpler way is to find a known anchor.
    # We will just append it if we can't do AST. But let's try to find "for (int m = 0; m < vNpc.size(); m++)"
    anchor = "for (int m = 0; m < vNpc.size(); m++)\n\t\t\t\t{\n\t\t\t\t\t((Npc)vNpc.elementAt(m)).paint(g);\n\t\t\t\t}"
    if anchor in content:
        content = content.replace(anchor, anchor + "\n\t\t\t\tCloudGarden.Paint(g);")
    else:
        # alternative anchor
        anchor = "((Npc)vNpc.elementAt(m)).paint(g);"
        if anchor in content:
            content = content.replace(anchor, anchor + "\n\t\t\t\t}\n\t\t\t\tCloudGarden.Paint(g);\n\t\t\t\t// dummy loop end {")

with open(gamescr_file, 'w', encoding='utf-8') as f:
    f.write(content)

print("Client patch complete.")
