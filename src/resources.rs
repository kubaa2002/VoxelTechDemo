use wgpu::{util::DeviceExt, Buffer};

use crate::texture;

pub async fn load_binary(file_name: &str) -> anyhow::Result<Vec<u8>> {
    let path = std::path::Path::new(env!("OUT_DIR"))
        .join("res")
        .join(file_name);
    let data = std::fs::read(path)?;

    Ok(data)
}

pub async fn load_texture(
    file_name: &str,
    device: &wgpu::Device,
    queue: &wgpu::Queue,
) -> anyhow::Result<texture::Texture> {
    let data = load_binary(file_name).await?;
    texture::Texture::from_bytes(device, queue, &data, file_name)
}
pub const CHUNK_SIZE: i32 = 64;

pub fn create_index_buffer(device: &wgpu::Device) -> Buffer {
    let mut indices: Vec<i32> = Vec::with_capacity((CHUNK_SIZE.pow(3)*36) as usize);
    let indices_offset: [i32; 6] = [0,1,2,1,3,2];
    for i in 0..CHUNK_SIZE.pow(3)*6 {
        for j in 0..6 {
            indices.push(i*4+indices_offset[j]);
        }
    }

    device.create_buffer_init(&wgpu::util::BufferInitDescriptor {
        label: Some("cube indices"),
        contents: bytemuck::cast_slice(&indices),
        usage: wgpu::BufferUsages::INDEX,
    })
}
