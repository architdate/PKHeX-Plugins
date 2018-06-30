/*
* Copyright 2007 ZXing authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
namespace com.google.zxing.common
{
	
	/// <summary> This is merely a clone of <code>Comparator</code> since it is not available in
	/// CLDC 1.1 / MIDP 2.0.
	/// </summary>
	public interface Comparator
	{
		
		int compare(System.Object o1, System.Object o2);
	}
}