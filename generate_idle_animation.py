"""
生成角色静止动画脚本
基于原始Player.png生成4帧静止动画（原始帧 + 3个后续帧）
"""

from PIL import Image
import os

def generate_idle_frames(input_path, output_dir=None):
    """
    生成4帧静止动画
    
    参数:
        input_path: 输入图像路径
        output_dir: 输出目录（默认为输入文件所在目录）
    """
    # 打开原始图像
    original_img = Image.open(input_path)
    
    # 确保图像是RGBA模式（支持透明度）
    if original_img.mode != 'RGBA':
        original_img = original_img.convert('RGBA')
    
    # 确定输出目录
    if output_dir is None:
        output_dir = os.path.dirname(input_path)
    
    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)
    
    # 获取基础文件名（不含扩展名）
    base_name = os.path.splitext(os.path.basename(input_path))[0]
    
    # 创建4帧动画
    frames = []
    
    # 第1帧：原始图像（直接复制）
    frame1 = original_img.copy()
    frames.append(frame1)
    
    # 计算像素尺寸（用于微调）
    width, height = original_img.size
    
    # 第2帧：轻微上移 + 轻微缩小（呼吸效果 - 吸气）
    # 轻微上移1像素，轻微缩小0.5%
    frame2 = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    scale = 0.995
    new_width = int(width * scale)
    new_height = int(height * scale)
    frame2_resized = original_img.resize((new_width, new_height), Image.NEAREST)
    offset_x = (width - new_width) // 2
    offset_y = (height - new_height) // 2 - 1  # 上移1像素
    frame2.paste(frame2_resized, (offset_x, offset_y), frame2_resized)
    frames.append(frame2)
    
    # 第3帧：回到中心，轻微放大（呼吸效果 - 呼气）
    # 轻微下移1像素，轻微放大0.5%
    frame3 = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    scale = 1.005
    new_width = int(width * scale)
    new_height = int(height * scale)
    frame3_resized = original_img.resize((new_width, new_height), Image.NEAREST)
    offset_x = (width - new_width) // 2
    offset_y = (height - new_height) // 2 + 1  # 下移1像素
    frame3.paste(frame3_resized, (offset_x, offset_y), frame3_resized)
    frames.append(frame3)
    
    # 第4帧：回到原始位置，与原图相似但略微不同（过渡回第1帧）
    frame4 = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    scale = 1.002
    new_width = int(width * scale)
    new_height = int(height * scale)
    frame4_resized = original_img.resize((new_width, new_height), Image.NEAREST)
    offset_x = (width - new_width) // 2
    offset_y = (height - new_height) // 2
    frame4.paste(frame4_resized, (offset_x, offset_y), frame4_resized)
    frames.append(frame4)
    
    # 保存所有帧
    saved_files = []
    for i, frame in enumerate(frames, 1):
        output_path = os.path.join(output_dir, f"{base_name}_idle_{i}.png")
        frame.save(output_path, 'PNG')
        saved_files.append(output_path)
        print(f"已生成第 {i} 帧: {output_path}")
    
    # 创建精灵表（所有帧横向排列）
    sprite_sheet_width = width * 4
    sprite_sheet_height = height
    sprite_sheet = Image.new('RGBA', (sprite_sheet_width, sprite_sheet_height), (0, 0, 0, 0))
    
    for i, frame in enumerate(frames):
        sprite_sheet.paste(frame, (i * width, 0))
    
    sprite_sheet_path = os.path.join(output_dir, f"{base_name}_idle_sheet.png")
    sprite_sheet.save(sprite_sheet_path, 'PNG')
    saved_files.append(sprite_sheet_path)
    print(f"已生成精灵表: {sprite_sheet_path}")
    
    return saved_files

if __name__ == "__main__":
    # 默认输入路径（相对于脚本位置）
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_input = os.path.join(script_dir, "Assets", "Player Module", "Player image", "Player.png")
    
    # 检查文件是否存在
    if os.path.exists(default_input):
        input_path = default_input
        print(f"使用默认路径: {input_path}")
    else:
        # 如果默认路径不存在，让用户输入
        input_path = input("请输入Player.png的完整路径: ").strip('"').strip("'")
        if not os.path.exists(input_path):
            print(f"错误: 文件不存在: {input_path}")
            exit(1)
    
    # 生成动画帧
    try:
        saved_files = generate_idle_frames(input_path)
        print(f"\n✅ 成功生成 {len(saved_files)} 个文件！")
        print("\n生成的文件列表:")
        for file in saved_files:
            print(f"  - {file}")
        print("\n💡 提示: 您可以在Unity中使用这些单独的帧文件，或使用精灵表文件导入为Sprite Sheet。")
    except Exception as e:
        print(f"❌ 生成失败: {e}")
        import traceback
        traceback.print_exc()




