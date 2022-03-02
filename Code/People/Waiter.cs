using System;
using Godot;
using System.Linq;
using Godot.Collections;

namespace Staff
{
	/**<summary>Staff member that takes and devivers orders</summary>*/
	public class Waiter : Person
	{
		public enum Goal
		{
			/**<summary>Waiter is just idling</summary>*/
			None,
			/**<summary>Take order from customer</summary>*/
			TakeOrder,
			/**<summary>Happends when waiter has to pretend to listen for a few seconds before giving order to kitchen</summary>*/
			ListenToCustomerForOrder,
			/**<summary>Give order to the kitchen</summary>*/
			PassOrder,
			/**<summary>Take cooked food from kitchen and give to the customer</summary>*/
			AcquireOrder,
			/**<summary>Give order to the customer</summary>*/
			DeliverOrder,
			/**<summary>Leave to the staff room(or kitchen)<para/>This goal can be overriten at any time</summary>*/
			Leave
		}
		/**<summary>Current order that waiter is carrying.<para/>Used for when order is canceled</summary>*/
		public int currentOrder = -1;

		public Goal CurrentGoal = Goal.None;

		private uint _loadedCustomerId = 0;

		public override bool ShouldUpdate => (base.ShouldUpdate && CurrentGoal != Goal.None) || Fired;

		public override bool IsFree => base.IsFree && (CurrentGoal == Goal.None || CurrentGoal == Goal.Leave);

		[Signal]
		public delegate void OnWaiterIsFree(Waiter waiter);

		/**<summary>Customer from whom to take or deliver to the order<para/>Note that this value is reset each time action is finished</summary>*/
		public Customer currentCustomer = null;

        /**<summary>Amount of bytes used by CafeObject + amount of bytes used by this object</summary>*/
        public new static uint SaveDataSize = 15u;

		public Waiter(uint id,Texture texture, Cafe cafe, Vector2 pos) : base(id,texture, new Vector2(128, 128), texture.GetSize(), cafe, pos, (int)ZOrderValues.Customer)
		{
			BeFree();

			Salary = 100;
			cafe.Connect(nameof(Cafe.OnCustomerArrivedAtTheTable),this,nameof(OnNewCustomerIsAtTheTable));
			cafe.Connect(nameof(Cafe.OnOderFinished),this,nameof(TakeNewCompletedOrder));
		}


		public Waiter(Cafe cafe,uint[] saveData) : base(cafe,saveData)
		{
			cafe.Connect(nameof(Cafe.OnCustomerArrivedAtTheTable),this,nameof(OnNewCustomerIsAtTheTable));
			cafe.Connect(nameof(Cafe.OnOderFinished),this,nameof(TakeNewCompletedOrder));
            Salary = 100;
			textureSize = cafe.TextureManager.GetTexture("Waiter").GetSize();
			size = new Vector2(128, 128);
            CurrentGoal = (Goal)saveData[12];
            currentOrder = (int)saveData[13] - 1;
            _loadedCustomerId = saveData[14];
			GenerateRIDBasedOnTexture(cafe.TextureManager.GetTexture("Waiter"), ZOrderValues.Customer);
		}

		public override Array<uint> GetSaveData()
		{
			Array<uint> data = base.GetSaveData();//total count of CafeObject is 5; total count of Person is 2
			data.Add((uint)CurrentGoal);//[12]
			data.Add((uint)(currentOrder < 0 ? 0 : currentOrder + 1));//[13]
			data.Add((currentCustomer?.Id + 1u ?? 0x00));//[14]
			return data;
		}

		public override	void SaveInit()
		{
			base.SaveInit();
			if(_loadedCustomerId > 0)
			{
				currentCustomer = cafe.People.OfType<Customer>().FirstOrDefault(p=>p.Id == _loadedCustomerId - 1u);
			}
		}

		public override void GetFired()
		{
			
			//first do proper cancelation or tasks so other waiters could 
			switch (CurrentGoal)
			{
				case Goal.TakeOrder:
				case Goal.PassOrder:
					//we reset by telling that new waiter needs to attend this customer
					cafe.AddNewArrivedCustomer(currentCustomer);
					break;
				case Goal.AcquireOrder:   
				case Goal.DeliverOrder:
					//reset order back to kitchen
					cafe.AddCompletedOrder(currentOrder);
					break;
				default:
					break;
			}
			base.GetFired();
		}

		public override void ResetOrCancelGoal(bool forceCancel = false)
		{
			base.ResetOrCancelGoal(forceCancel);
			switch (CurrentGoal)
			{
				case Goal.DeliverOrder:
					//this means that person who ordered their food has been moved or lost their table

					//customer has been left table-less
					if (currentCustomer.CurrentTableId == -1 || forceCancel)
					{
						cafe.CompletedOrders.Add(currentOrder);
						BeFree();
					}
					else
					{
						PathToTheTarget = cafe.Navigation.FindPathTo(Position, cafe.GetFurniture((uint)currentCustomer.CurrentTableId).Position);
					}
					break;
				case Goal.TakeOrder:
					if (currentCustomer != null)
					{
						//table was moved and no other were found
						if (currentCustomer.CurrentTableId == -1 || forceCancel)
						{
							currentCustomer = null;
							BeFree();
						}
						else
						{
							PathToTheTarget = cafe.Navigation.FindPathTo(Position,cafe.GetFurniture((uint)currentCustomer.CurrentTableId).Position);
						}
					}
					break;
					//those two tasks are for going towards kitchen
				case Goal.AcquireOrder:
				case Goal.PassOrder:
					if (forceCancel)
					{
						//this goal is not meant to be reset in this version of the game  and as such this does nothing
						GD.PrintErr("Warning: Attempted to cancel task when going towards kitchen. This should not happen and will be ignored");
					}
					else
					{
						PathToTheTarget = cafe.Navigation.FindLocation("Kitchen", Position);
					}
					break;
				default:
					//it's not like we were doing anything anyway
					break;
			}
		}

		private void BeFree()
		{
			//waiter is now free
			CurrentGoal = Goal.None;
            if (currentCustomer != null)
            {
                (cafe.GetFurniture((uint)currentCustomer.CurrentTableId)).CurrentUser = null;
                //forget about this customer
                currentCustomer = null;
            }
			//are there any finished orders?
			if (cafe.CompletedOrders.Any())
			{
				TakeNewCompletedOrder();
			}
			//if not check if there are any waiting customers
			else if (cafe.CustomersToTakeOrderFrom.Any())
			{
				OnNewCustomerIsAtTheTable();
			}
			else
			{
				//move waiter to "staff location"
				PathToTheTarget = cafe.Navigation.FindLocation("Kitchen", Position);
				CurrentGoal = Goal.Leave;
			}
		}

		void changeTask(Vector2 target, Waiter.Goal goal, Customer customer)
		{
			PathToTheTarget = cafe.Navigation.GetSimplePath(Position, target) ?? throw new NullReferenceException("Failed to find path to the task!");
			CurrentGoal = goal;
			currentCustomer = customer;
		}

        public void OnNewCustomerIsAtTheTable()
        {
            if (cafe.CustomersToTakeOrderFrom.Any() && IsFree)
            {
                Furniture table = cafe.GetFurniture((uint)(cafe.People[cafe.CustomersToTakeOrderFrom.First()] as Customer).CurrentTableId);
                PathToTheTarget = cafe.Navigation.GetSimplePath(Position, table.Position);
                if (pathToTheTarget != null)
                {
                    CurrentGoal = Goal.TakeOrder;
                    currentCustomer = table.CurrentCustomer;
                    table.CurrentUser = this;
                    cafe.CustomersToTakeOrderFrom.RemoveAt(0);
                }
            }
        }

		public void TakeNewCompletedOrder()
		{
            if (cafe.CompletedOrders.Any() && IsFree)
            {
				//because we prioritize customers who arrived early(otherwise some of them might not get served their food at all)
				//we have to take first
				int orderId = cafe.CompletedOrders.First();
				
                var target = cafe.People.Values.OfType<Customer>().FirstOrDefault(p => p.IsWaitingForOrder(orderId));
                if (target != null)
                {
                    CurrentGoal = Waiter.Goal.AcquireOrder;
                    currentOrder = orderId;
                    PathToTheTarget = cafe.Navigation.FindLocation("Kitchen", Position);
                    currentCustomer = target;
					target.CurrentWaiter = this;
                    cafe.CompletedOrders.Remove(0);
                }
				else
				{
					GD.PrintErr("Order was completed but waiter found no customer for it!");
				}
            }
		}

		protected override void OnTaskTimerRunOut()
		{
			CurrentGoal = Goal.PassOrder;

			//don't reset current customer because we still need to know the order
			//find path to the kitchen
			PathToTheTarget = cafe.Navigation.FindLocation("Kitchen", Position);
			currentCustomer.OnOrderTaken();	
		}

		protected override void onArrivedToTheTarget()
		{
			base.onArrivedToTheTarget();
			if (!Fired)
			{
				switch (CurrentGoal)
				{
					case Goal.TakeOrder:
                        CurrentGoal = Goal.ListenToCustomerForOrder;
                        //wait fore a few second to make it look like waiter is listening
                        //once timer runs out waiter will switch state to the next one
                        SetTaskTimer(currentCustomer.OrderTime);
                        break;
                    case Goal.PassOrder:
                        //kitchen is now making the order                           
                        cafe.AddNewOrder(currentCustomer.OrderId);
                        BeFree();
                        break;
                    case Goal.AcquireOrder:
                        //make way towards customer now
                        PathToTheTarget = cafe.Navigation.FindPathTo(Position, currentCustomer.Position);
                        CurrentGoal = Goal.DeliverOrder;
                        break;
                    case Goal.DeliverOrder:
                        currentCustomer.Eat();
                        GD.Print($"Feeding: {currentCustomer.ToString()}");

                        BeFree();
                        break;

                    case Goal.Leave:
                        CurrentGoal = Goal.None;
                        break;
                }
            }

        }
	}
}
