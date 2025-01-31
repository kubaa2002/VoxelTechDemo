use wgpu::util::DeviceExt;

use crate::{model, texture};

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

pub async fn create_chunk_mesh(
    device: &wgpu::Device,
    name: &str,
    queue: &wgpu::Queue,
    layout: &wgpu::BindGroupLayout,
    x: f32,
    y: f32,
    z: f32,
) -> model::ChunkMesh {
    let vertices: [model::ModelVertex; 24] = [
        // North face vertices
        model::ModelVertex { position: [x, y, z+1.0], tex_coords: [0.0, 0.0] },
        model::ModelVertex { position: [x+1.0, y, z+1.0], tex_coords: [1.0, 0.0] },
        model::ModelVertex { position: [x+1.0, y+1.0, z+1.0], tex_coords: [1.0, 1.0] },
        model::ModelVertex { position: [x, y+1.0, z+1.0], tex_coords: [0.0, 1.0] },
    
        // South face vertices
        model::ModelVertex { position: [x+1.0, y, z], tex_coords: [0.0, 0.0] },
        model::ModelVertex { position: [x, y, z], tex_coords: [1.0, 0.0] },
        model::ModelVertex { position: [x, y+1.0, z], tex_coords: [1.0, 1.0] },
        model::ModelVertex { position: [x+1.0, y+1.0, z], tex_coords: [0.0, 1.0] },

        // West face vertices
        model::ModelVertex { position: [x+1.0, y, z+1.0], tex_coords: [1.0, 0.0] },
        model::ModelVertex { position: [x+1.0, y, z], tex_coords: [0.0, 0.0] },
        model::ModelVertex { position: [x+1.0, y+1.0, z], tex_coords: [0.0, 1.0] },
        model::ModelVertex { position: [x+1.0, y+1.0, z+1.0], tex_coords: [1.0, 1.0] },
    
        // East face vertices
        model::ModelVertex { position: [x, y, z], tex_coords: [1.0, 0.0] },
        model::ModelVertex { position: [x, y, z+1.0], tex_coords: [0.0, 0.0] },
        model::ModelVertex { position: [x, y+1.0, z+1.0], tex_coords: [0.0, 1.0] },
        model::ModelVertex { position: [x, y+1.0, z], tex_coords: [1.0, 1.0] },

        // Top face vertices
        model::ModelVertex { position: [x+1.0, y+1.0, z], tex_coords: [1.0, 0.0] },
        model::ModelVertex { position: [x, y+1.0, z], tex_coords: [0.0, 0.0] },
        model::ModelVertex { position: [x, y+1.0, z+1.0], tex_coords: [0.0, 1.0] },
        model::ModelVertex { position: [x+1.0, y+1.0, z+1.0], tex_coords: [1.0, 1.0] },
    
        // Bottom face vertices
        model::ModelVertex { position: [x, y, z], tex_coords: [1.0, 0.0] },
        model::ModelVertex { position: [x+1.0, y, z], tex_coords: [0.0, 0.0] },
        model::ModelVertex { position: [x+1.0, y, z+1.0], tex_coords: [0.0, 1.0] },
        model::ModelVertex { position: [x, y, z+1.0], tex_coords: [1.0, 1.0] },
    ];

    let indices: [u32; 36] = [
        // North face
        0, 1, 2, 2, 3, 0,

        // South face
        4, 5, 6, 6, 7, 4,
        
        // West face
        8, 9, 10, 10, 11, 8,

        // East face
        12, 13, 14, 14, 15, 12,

        // Top face
        16, 17, 18, 18, 19, 16,

        // Bottom face
        20, 21, 22, 22, 23, 20,
    ];
    let vertex_buffer = device.create_buffer_init(&wgpu::util::BufferInitDescriptor {
        label: Some("cube vertices"),
        contents: bytemuck::cast_slice(&vertices),
        usage: wgpu::BufferUsages::VERTEX,
    });
    let index_buffer = device.create_buffer_init(&wgpu::util::BufferInitDescriptor {
        label: Some("cube indices"),
        contents: bytemuck::cast_slice(&indices),
        usage: wgpu::BufferUsages::INDEX,
    });

    let texture = load_texture("Cobblestone.png", device, queue).await.unwrap();

    model::ChunkMesh::new(device, name, texture, layout, vertex_buffer, index_buffer, indices)
}
