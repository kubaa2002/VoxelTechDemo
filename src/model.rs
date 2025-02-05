use crate::{resources, texture::Texture};
use crate::resources::CHUNK_SIZE;
use wgpu::util::DeviceExt;
use crate::open_simplex::noise2;
pub trait Vertex {
    fn desc() -> wgpu::VertexBufferLayout<'static>;
}

#[repr(C)]
#[derive(Copy, Clone, Debug, bytemuck::Pod, bytemuck::Zeroable)]
pub struct ModelVertex {
    pub position: [f32; 3],
    pub tex_coords: [f32; 2],
}

impl Vertex for ModelVertex {
    fn desc() -> wgpu::VertexBufferLayout<'static> {
        use std::mem;
        wgpu::VertexBufferLayout {
            array_stride: mem::size_of::<ModelVertex>() as wgpu::BufferAddress,
            step_mode: wgpu::VertexStepMode::Vertex,
            attributes: &[
                wgpu::VertexAttribute {
                    offset: 0,
                    shader_location: 0,
                    format: wgpu::VertexFormat::Float32x3,
                },
                wgpu::VertexAttribute {
                    offset: mem::size_of::<[f32; 3]>() as wgpu::BufferAddress,
                    shader_location: 1,
                    format: wgpu::VertexFormat::Float32x2,
                },
            ],
        }
    }
}

pub struct Material {
    pub bind_group: wgpu::BindGroup,
}

impl Material {
    pub fn new(
        device: &wgpu::Device,
        name: &str,
        texture: Texture,
        layout: &wgpu::BindGroupLayout,
    ) -> Self {
        let bind_group = device.create_bind_group(&wgpu::BindGroupDescriptor {
            layout,
            entries: &[
                wgpu::BindGroupEntry {
                    binding: 0,
                    resource: wgpu::BindingResource::TextureView(&texture.view),
                },
                wgpu::BindGroupEntry {
                    binding: 1,
                    resource: wgpu::BindingResource::Sampler(&texture.sampler),
                },
            ],
            label: Some(name),
        });

        Self {
            bind_group,
        }
    }
}

pub struct Chunk {
    pub vertex_buffer: Option<wgpu::Buffer>,
    pub num_elements: Option<u32>,
    pub blocks: Vec<u8>,
}
impl Chunk {
    pub fn new() -> Self {
        Self {
            vertex_buffer: None,
            num_elements: None,
            blocks: vec![0; resources::CHUNK_SIZE.pow(3) as usize]
        }
    }
    pub fn create_chunk_mesh(
        &mut self,
        device: &wgpu::Device,
        mut chunk_x: f32,
        mut chunk_y: f32,
        mut chunk_z: f32,
    ){
        chunk_x *= CHUNK_SIZE as f32;
        chunk_y *= CHUNK_SIZE as f32;
        chunk_z *= CHUNK_SIZE as f32;
    
        let mut vertices: Vec<ModelVertex> = Vec::new();
        let tex_coords: [[f32; 2]; 4] = [[0.0,0.0],[0.0,1.0],[1.0,0.0],[1.0,1.0]];
        let offset_x = 0b1010_0101_1010_1010_0000_1111;
        let offset_y = 0b1100_1100_0000_1111_1100_1100;
        let offset_z = 0b0000_1111_0011_1100_0101_1010;
    
        let mut currentblock = 0;
        for z in 0..CHUNK_SIZE {
            for y in 0..CHUNK_SIZE {
                for x in 0..CHUNK_SIZE {
                    if self.blocks[currentblock] != 0 {
                        let mut faces = 0;
                        if (x != (CHUNK_SIZE-1) && self.blocks[currentblock+1] == 0) || x == CHUNK_SIZE-1{
                            faces |= 1;
                        }
                        if (x != 0 && self.blocks[currentblock-1] == 0) || x == 0 {
                            faces |= 2;
                        }
                        if (y != (CHUNK_SIZE-1) && self.blocks[currentblock+CHUNK_SIZE as usize] == 0) || y == CHUNK_SIZE-1 {
                            faces |= 4;
                        }
                        if (y != 0 && self.blocks[currentblock-CHUNK_SIZE as usize] == 0) || y == 0 {
                            faces |= 8;
                        }
                        if (z != (CHUNK_SIZE-1) && self.blocks[currentblock+CHUNK_SIZE.pow(2) as usize] == 0) || z == CHUNK_SIZE-1 {
                            faces |= 16;
                        }
                        if (z != 0 && self.blocks[currentblock-CHUNK_SIZE.pow(2) as usize] == 0) || z == 0 {
                            faces |= 32;
                        }
                        
                        if faces != 0{
                            for face in 0..6{
                                if (faces&1)!=0{
                                    for i in face*4..face*4+4 {
                                        vertices.push(ModelVertex{
                                            position: [
                                                chunk_x+(x as f32)+(((offset_x>>i)&1) as f32),
                                                chunk_y+(y as f32)+(((offset_y>>i)&1) as f32),
                                                chunk_z+(z as f32)+(((offset_z>>i)&1) as f32)
                                            ],
                                            tex_coords: tex_coords[i%4]
                                        });
                                    }
                                }
                                faces>>=1;
                            }
                        }
                    }
                    currentblock += 1;
                }
            }
        }
    
        if vertices.len() > 0 {
            self.vertex_buffer = Some(device.create_buffer_init(&wgpu::util::BufferInitDescriptor {
                label: Some("cube vertices"),
                contents: bytemuck::cast_slice(&vertices),
                usage: wgpu::BufferUsages::VERTEX,
            }));
    
            self.num_elements = Some((vertices.len()*3/2) as u32);
        }
    }
    
    pub fn generate_terrain(&mut self, mut chunk_x: i32, mut chunk_y: i32, mut chunk_z: i32){
        chunk_x *= CHUNK_SIZE;
        chunk_y *= CHUNK_SIZE;
        chunk_z *= CHUNK_SIZE;
        for x in 0..CHUNK_SIZE {
            for z in 0..CHUNK_SIZE {
                let noise = (noise2(12345, ((x+chunk_x) as f64)/2000.0, ((z+chunk_z) as f64)/2000.0)*5.0
                    + noise2(12345, ((x+chunk_x) as f64)/400.0, ((z+chunk_z) as f64)/400.0)*3.0
                    + noise2(12345, ((x+chunk_x) as f64)/100.0, ((z+chunk_z) as f64)/100.0)*0.5).powi(2) as i32 + 50;
                if noise >= chunk_y {
                    let ylevel: i32;
                    if noise < chunk_y+CHUNK_SIZE {
                        ylevel = noise-chunk_y;
                    }
                    else {
                        ylevel = CHUNK_SIZE;
                    }
                    for y in 0..ylevel {
                        self.blocks[(x+y*CHUNK_SIZE+z*CHUNK_SIZE*CHUNK_SIZE) as usize] = 1;
                    }
                }
            }
        }
    }
}

pub trait DrawModel<'a> {
    fn draw_mesh_instanced(
        &mut self,
        mesh: &'a Chunk,
    );
}

impl<'a, 'b> DrawModel<'b> for wgpu::RenderPass<'a>
where
    'b: 'a,
{
    fn draw_mesh_instanced(
        &mut self,
        mesh: &'b Chunk,
    ) {
        if mesh.vertex_buffer.is_some() {
            self.set_vertex_buffer(0, mesh.vertex_buffer.as_ref().unwrap().slice(..));
            self.draw_indexed(0..mesh.num_elements.unwrap(), 0, 0..1);
        }
    }
}