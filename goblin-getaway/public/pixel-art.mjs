function loadSprite(name){
 if(typeof Image!=='function')return null;
 const image=new Image();
 image.src=new URL(`./pixel-art/${name}.png`,import.meta.url).href;
 return image;
}

const stills=[
 'bottom','bottom_bomb','bottom_bombbox','bottom_bombplus','bottom_cannon',
 'bottom_cannon_aim_count','bottom_cannon_aim_count_bar','bottom_cannonbar','bottom_cannonshot',
 'bottom_cellar','bottom_crown','bottom_horse_move','bottom_paper','bottom_repair','bottom_shadow',
 'bottom_sofa','enemy','enemy_spear','horse1','horse_equip','royal-children','draft-horse-walk','rack-shell','supply-pile',
 'upper-woodland','upper-shoulder','upper-verge','road-surface','road-specks','lower-woodland',
 'lower-shoulder','lower-verge','roadside-tree','road-rock','road-deer','road-log','wagon-cellar',
 'catapult-rock','catapult-landing-danger','catapult-landing-safe','catapult-impact','cannon-scorch','cannon-muzzle-smoke',
 'RoadWarning','EmoteBubble','EmoteAmmo','EmoteLoad','EmoteRepair','EmoteDrive','EmoteCare','EmoteGood','EmoteSurprised',
];
const sequences=[
 'bottom_blue','bottom_cannon_aim','bottom_cannon_aim_count_fire','bottom_cannon_bomb',
 'bottom_cannon_bomb_effect','bottom_cannon_fier','bottom_flag','bottom_light','bottom_orange',
 'bottom_pink','bottom_purple','bottom_wheel','bottom_wheel_bottomequip','bottom_wheel_dust',
 'bottom_wheel_out','bottom_wheel_out_effect','bottom_wheel_topequip',
];

// Web sprites include the original PNGs from Assets/resource and the Unity pixel art.
export const art=Object.fromEntries([
 ...stills.map(name=>[name,loadSprite(name)]),
 ...sequences.map(name=>[name,Array.from({length:4},(_,i)=>loadSprite(name+(i+1)))]),
]);

export const emoteArt={
 '💣':art.EmoteAmmo,'🔄':art.EmoteLoad,'🛠️':art.EmoteRepair,'🐎':art.EmoteDrive,
 '🍼':art.EmoteCare,'👍':art.EmoteGood,'😮':art.EmoteSurprised,
};

export function frame(images,tick,heldTicks=1){
 const index=Math.floor(tick/heldTicks)%images.length;
 return images[(index+images.length)%images.length];
}

export function sprite(c,image,x,y,width,height,source){
 if(!image?.complete||!image.naturalWidth)return false;
 const [sx,sy,sw,sh]=source??[0,0,image.naturalWidth,image.naturalHeight];
 height??=width*sh/sw;
 const smoothing=c.imageSmoothingEnabled;
 c.imageSmoothingEnabled=false;
 c.drawImage(image,sx,sy,sw,sh,x-width/2,y-height/2,width,height);
 c.imageSmoothingEnabled=smoothing;
 return true;
}
