const LEGACY_LANES = [145, 182, 219];
const profiles = {
  log: { halfWidth: 30, damage: 6, slowFactor: .52, slowSeconds: 1.65, name: '통나무' },
  rock: { halfWidth: 31, damage: 11, slowFactor: .76, slowSeconds: .95, name: '바위' },
  deer: { halfWidth: 28, damage: 4, slowFactor: .60, slowSeconds: 1.3, name: '사슴' },
};

export const ROAD_OBSTACLE_TYPES = Object.keys(profiles);
export const roadObstacleProfile = type => Object.hasOwn(profiles, type) ? profiles[type] : profiles.rock;
export const obstacleY = obstacle => obstacle.y ?? LEGACY_LANES[obstacle.lane] ?? LEGACY_LANES[1];

export function roadsideTrees(road,top,bottom){
  const trees=[];
  const spacing=270;
  for(const [side,edge] of [top,bottom].entries()){
    const offset=115+side*135;
    const first=Math.ceil((-135-road-offset)/spacing);
    const last=Math.floor((1460-road-offset)/spacing);
    for(let index=first;index<=last;index++){
      const x=road+offset+index*spacing;
      const shoulder=24+Math.sin(index*2.31+side)*8;
      trees.push({id:'tree:'+side+':'+index,x,y:edge+(side?-shoulder:shoulder),r:12});
    }
  }
  return trees;
}
