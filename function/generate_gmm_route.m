function quatRef=generate_gmm_route(Data,tau,dt)

addpath('../fcts/');
len=round(tau / dt);%表格一共有多少个点

model.nbStates = 5; %Number of states in the GMM
model.nbVar =3;     %Number of variables [t,wx,wy,wz,ax,ay,az]
model.dt = dt;    %Time step duration
nbData = len;      %Length of each trajectory

model = init_GMM_timeBased(Data, model);
model = EM_GMM(Data, model);
[DataOut, SigmaOut] = GMR(model, [1:nbData]*model.dt, 1, 2:model.nbVar); 

% %绘制高斯曲线
% subplot(1,3,1);
% for i=1:9
%     plot(newdata(1,:),newdata(1+i,:),"-");
%     hold on;
% end
% 
% plot(newdata(1,:),DataOut(1,:),"-",'linewidth',3);

for i=1:nbData
    quatRef(i).t=i*model.dt;
    quatRef(i).mu=DataOut(:,i);
    quatRef(i).sigma=SigmaOut(:,:,i);
end
end