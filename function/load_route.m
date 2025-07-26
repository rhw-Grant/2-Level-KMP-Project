function [Data,newdata]=load_route(path,tau,dt,N)

len=round(tau / dt);%表格一共有多少个点

%打开路径文件
data=csvread(path,1,0);
[row,col]=size(data);

for i=1:row
    for j=1:col
        newdata(j,i)=data(i,j);
    end
end
[row,col]=size(newdata);
for i=1:col
    newdata(1,i)=newdata(1,i)*dt;
end

Data=[]; % save transformed data
for i=1:N
    route(1,:)=data(:,1)'*dt;
    route(2,:)=data(:,1+i)';

    zeta(1:2,:)=route;
    zeta(3,:)=gradient(zeta(2,:))/dt;

    Data=[Data zeta];
end
end