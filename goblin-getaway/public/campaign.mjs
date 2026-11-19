// The prototype waves use variations of the existing cavalry until the roster is supplied.
export const ENEMY_PROFILES = {
 cavalry:{kind:0,speed:20,damage:14},
 scout:{kind:1,speed:26.4,damage:10},
 guard:{kind:2,speed:19,damage:18}
};

export const WAVE_ENEMY_MULTIPLIER=3;
const wave=(at,groups)=>({
 at,
 groups:Object.entries(groups).map(([enemy,count])=>({enemy,count:count*WAVE_ENEMY_MULTIPLIER}))
});

export const CHAPTERS = [
 {
  title:'왕국 국경 탈출',
  stages:[
   {
    title:'가시숲 고갯길',
    goal:1500,
    catapultFastFromWave:3,
    waves:[
     wave(4,{cavalry:2}),
     wave(16,{cavalry:2,scout:1}),
     wave(28,{cavalry:1,scout:2}),
     wave(40,{cavalry:2,scout:1,guard:1}),
     wave(52,{cavalry:2,scout:2,guard:1})
    ]
   },
   {
    title:'잿빛 돌길',
    goal:500,
    waves:[
     wave(4,{cavalry:2,scout:1}),
     wave(15,{cavalry:2,guard:1}),
     wave(26,{cavalry:2,scout:2}),
     wave(37,{cavalry:2,scout:1,guard:2}),
     wave(48,{cavalry:2,scout:2,guard:1}),
     wave(59,{cavalry:2,scout:2,guard:2})
    ]
   },
   {
    title:'황혼의 국경',
    goal:500,
    waves:[
     wave(3,{cavalry:2,scout:1}),
     wave(13,{cavalry:2,scout:1,guard:1}),
     wave(23,{cavalry:2,scout:2,guard:1}),
     wave(33,{cavalry:2,scout:1,guard:2}),
     wave(43,{cavalry:2,scout:2,guard:2}),
     wave(53,{cavalry:1,scout:2,guard:3}),
     wave(63,{cavalry:2,scout:3,guard:2})
    ]
   }
  ]
 }
];

export function chapterSpec(chapter=1){
 return CHAPTERS[chapter-1]??CHAPTERS[0];
}

export function stageSpec(chapter=1,stage=1){
 const {stages}=chapterSpec(chapter);
 return stages[stage-1]??stages[0];
}

export function nextStagePosition(chapter,stage){
 const current=CHAPTERS[chapter-1];
 if(!current?.stages[stage-1])return null;
 if(current.stages[stage])return {chapter,stage:stage+1};
 if(CHAPTERS[chapter]?.stages.length)return {chapter:chapter+1,stage:1};
 return null;
}
