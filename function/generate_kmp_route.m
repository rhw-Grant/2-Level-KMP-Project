function kmpTraj1=generate_kmp_route(quatRef,tau,dt)
len=round(tau / dt);%表格一共有多少个点

%我就是不写标注，反正我看得懂
% 设置KMP模型参数
lamda=1;
kh=0.1;
dim=1;

via_var=1E-10*eye(2);
via_time=[];



%% Update the reference trajectory using transformed desired points
interval=5; % speed up the computation
num=round(len/interval)+1;
for i=1:num
    if i==1 index=1;
    else index=(i-1)*interval;
    end
    sampleData(i).t=quatRef(index).t;
    sampleData(i).mu=quatRef(index).mu;
    sampleData(i).sigma=quatRef(index).sigma; 
end



%% KMP prediction
Kinv = kmp_estimateMatrix_mean(sampleData,num,kh,lamda,dim);


for index=1:len
    t=index*dt;
    mu=kmp_pred_mean(t,sampleData,num,kh,Kinv,dim);    
    kmpTraj1(1,index)=t;
    kmpTraj1(2:3,index)=mu;
end

timeplot=linspace(0, tau, len);
plot(timeplot,kmpTraj1(2,:),"-",'linewidth',3);
hold on;


end