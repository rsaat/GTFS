select t1.id from trip t1
where t1.route_id in (
select t.route_id from trip t
GROUP BY t.route_id,t.service_id
Having COUNT(DISTINCT t.id)>1 and   COUNT(DISTINCT t.id_stop_time)=1)
AND t1.id IN ('4718-10-1')


